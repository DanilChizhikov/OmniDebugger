#if OMNI_DEBUGGER
using System.Collections.Generic;
using System.Text;
using DTech.OmniDebugger.UI;
using UnityEditor;
using UnityEngine;

namespace DTech.OmniDebugger.Editor
{
	[CustomPropertyDrawer(typeof(OmniDebuggerShortcut))]
	internal sealed class ShortcutDrawer : PropertyDrawer
	{
		public const string KeysField = "_keys";
		public const float SideButtonWidth = 22.0f;
		public const float Spacing = 2.0f;

		private const string EmptyText = "None — click to bind";
		private const string WaitingText = "Hold the keys, then release…";
		private const string Separator = " + ";

		private static readonly Color _recordingTint = new Color(0.18f, 0.77f, 0.71f);
		private static readonly GUIContent _clearContent = new GUIContent("×", "Clear the keys.");
		private static readonly List<KeyCode> _pending = new ();
		private static readonly StringBuilder _text = new ();

		private static string _recording;
		private static bool _suppressingShortcuts;

		public static void DrawBinding(Rect rect, SerializedProperty shortcut)
		{
			SerializedProperty keys = shortcut.FindPropertyRelative(KeysField);
			string owner = GetOwner(shortcut);
			int id = GUIUtility.GetControlID(FocusType.Keyboard, rect);

			if (_recording == owner)
			{
				Record(id, rect, keys);
			}

			bool recording = _recording == owner;
			string text = recording
				? _pending.Count == 0 ? WaitingText : Format(Order(_pending))
				: HasKeys(keys) ? Format(keys) : EmptyText;

			Color previous = GUI.backgroundColor;

			if (recording)
			{
				GUI.backgroundColor = _recordingTint;
			}

			bool clicked = GUI.Button(rect, text);
			GUI.backgroundColor = previous;

			if (clicked && !recording)
			{
				StartRecording(owner, id);
			}
		}

		public static void StopRecording()
		{
			_recording = null;
			_pending.Clear();

			if (_suppressingShortcuts)
			{
				EditorGUIUtility.editingTextField = false;
				_suppressingShortcuts = false;
			}
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			position = string.IsNullOrEmpty(label?.text)
				? EditorGUI.IndentedRect(position)
				: EditorGUI.PrefixLabel(position, label);

			Rect field = new Rect(position.x, position.y, position.width - SideButtonWidth - Spacing, position.height);
			Rect clear = new Rect(field.xMax + Spacing, position.y, SideButtonWidth, position.height);

			DrawBinding(field, property);

			SerializedProperty keys = property.FindPropertyRelative(KeysField);

			using (new EditorGUI.DisabledScope(!HasKeys(keys)))
			{
				if (GUI.Button(clear, _clearContent, EditorStyles.miniButton))
				{
					StopRecording();
					keys.arraySize = 0;
				}
			}
		}

		private static string GetOwner(SerializedProperty property) =>
			$"{property.serializedObject.targetObject.GetInstanceID()}:{property.propertyPath}";

		private static void StartRecording(string owner, int id)
		{
			StopRecording();
			_recording = owner;
			GUIUtility.keyboardControl = id;

			if (!EditorGUIUtility.editingTextField)
			{
				EditorGUIUtility.editingTextField = true;
				_suppressingShortcuts = true;
			}
		}

		private static void Record(int id, Rect rect, SerializedProperty keys)
		{
			Event evt = Event.current;

			if (evt.rawType == EventType.MouseDown)
			{
				if (!rect.Contains(evt.mousePosition))
				{
					Finish(id, _pending.Count > 0 ? keys : null);
				}
				else if (evt.type == EventType.MouseDown)
				{
					evt.Use();
				}

				return;
			}

			GUIUtility.keyboardControl = id;

			switch (evt.GetTypeForControl(id))
			{
				case EventType.KeyDown:
					if (evt.keyCode == KeyCode.Escape)
					{
						Finish(id, null);
					}
					else if (evt.keyCode != KeyCode.None)
					{
						AddModifiers(evt.modifiers);
						Add(evt.keyCode);
					}

					evt.Use();
					HandleUtility.Repaint();
					break;

				case EventType.KeyUp:
					if (_pending.Count > 0)
					{
						Finish(id, keys);
					}

					evt.Use();
					HandleUtility.Repaint();
					break;
			}
		}

		private static void Finish(int id, SerializedProperty keys)
		{
			if (keys != null)
			{
				List<KeyCode> chord = Order(_pending);
				keys.arraySize = chord.Count;

				for (int i = 0; i < chord.Count; i++)
				{
					keys.GetArrayElementAtIndex(i).intValue = (int)chord[i];
				}

				GUI.changed = true;
			}

			if (GUIUtility.keyboardControl == id)
			{
				GUIUtility.keyboardControl = 0;
			}

			StopRecording();
		}

		private static void AddModifiers(EventModifiers modifiers)
		{
			if ((modifiers & EventModifiers.Control) != 0)
			{
				Add(KeyCode.LeftControl);
			}

			if ((modifiers & EventModifiers.Shift) != 0)
			{
				Add(KeyCode.LeftShift);
			}

			if ((modifiers & EventModifiers.Alt) != 0)
			{
				Add(KeyCode.LeftAlt);
			}

			if ((modifiers & EventModifiers.Command) != 0)
			{
				Add(Application.platform == RuntimePlatform.OSXEditor ? KeyCode.LeftCommand : KeyCode.LeftWindows);
			}
		}

		private static void Add(KeyCode key)
		{
			key = ToLeft(key);

			if (!_pending.Contains(key))
			{
				_pending.Add(key);
			}
		}

		private static KeyCode ToLeft(KeyCode key)
		{
			switch (key)
			{
				case KeyCode.RightShift: return KeyCode.LeftShift;
				case KeyCode.RightControl: return KeyCode.LeftControl;
				case KeyCode.RightAlt: return KeyCode.LeftAlt;
				case KeyCode.RightCommand: return KeyCode.LeftCommand;
				case KeyCode.RightWindows: return KeyCode.LeftWindows;
				default: return key;
			}
		}

		private static List<KeyCode> Order(IReadOnlyList<KeyCode> keys)
		{
			List<KeyCode> ordered = new List<KeyCode>(keys.Count);

			for (int rank = 0; rank <= 4; rank++)
			{
				for (int i = 0; i < keys.Count; i++)
				{
					if (keys[i] != KeyCode.None && Rank(keys[i]) == rank)
					{
						ordered.Add(keys[i]);
					}
				}
			}

			return ordered;
		}

		private static int Rank(KeyCode key)
		{
			switch (ToLeft(key))
			{
				case KeyCode.LeftControl: return 0;
				case KeyCode.LeftShift: return 1;
				case KeyCode.LeftAlt: return 2;
				case KeyCode.LeftCommand:
				case KeyCode.LeftWindows: return 3;
				default: return 4;
			}
		}

		private static bool HasKeys(SerializedProperty keys)
		{
			for (int i = 0; i < keys.arraySize; i++)
			{
				if (keys.GetArrayElementAtIndex(i).intValue != (int)KeyCode.None)
				{
					return true;
				}
			}

			return false;
		}

		private static string Format(SerializedProperty keys)
		{
			List<KeyCode> chord = new List<KeyCode>(keys.arraySize);

			for (int i = 0; i < keys.arraySize; i++)
			{
				chord.Add((KeyCode)keys.GetArrayElementAtIndex(i).intValue);
			}

			return Format(Order(chord));
		}

		private static string Format(IReadOnlyList<KeyCode> keys)
		{
			_text.Clear();

			for (int i = 0; i < keys.Count; i++)
			{
				if (_text.Length > 0)
				{
					_text.Append(Separator);
				}

				_text.Append(GetName(keys[i]));
			}

			return _text.ToString();
		}

		private static string GetName(KeyCode key)
		{
			if (key >= KeyCode.Alpha0 && key <= KeyCode.Alpha9)
			{
				return ((char)('0' + (key - KeyCode.Alpha0))).ToString();
			}

			if (key >= KeyCode.F1 && key <= KeyCode.F15)
			{
				return key.ToString();
			}

			switch (key)
			{
				case KeyCode.LeftControl:
				case KeyCode.RightControl: return "Ctrl";
				case KeyCode.LeftShift:
				case KeyCode.RightShift: return "Shift";
				case KeyCode.LeftAlt:
				case KeyCode.RightAlt: return "Alt";
				case KeyCode.LeftCommand:
				case KeyCode.RightCommand: return "Cmd";
				case KeyCode.LeftWindows:
				case KeyCode.RightWindows: return "Win";
				case KeyCode.Return: return "Enter";
				case KeyCode.BackQuote: return "`";
				case KeyCode.Minus: return "-";
				case KeyCode.Equals: return "=";
				case KeyCode.LeftBracket: return "[";
				case KeyCode.RightBracket: return "]";
				case KeyCode.Semicolon: return ";";
				case KeyCode.Quote: return "'";
				case KeyCode.Comma: return ",";
				case KeyCode.Period: return ".";
				case KeyCode.Slash: return "/";
				case KeyCode.Backslash: return "\\";
				default: return ObjectNames.NicifyVariableName(key.ToString());
			}
		}
	}
}
#endif