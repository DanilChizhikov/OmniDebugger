#if OMNI_DEBUGGER
using System.Collections.Generic;
using DTech.OmniDebugger.UI;
using UnityEditor;
using UnityEngine;

namespace DTech.OmniDebugger.Editor
{
	internal static class OmniDebuggerPanelSettingsProvider
	{
		private const string SettingsPath = "Project/DTech/OmniDebugger/Panel";

		private const string CreatePanelField = "_createPanel";
		private const string PanelField = "_panel";
		private const string DefaultThemeField = "_defaultTheme";
		private const string ThemesField = "_themes";
		private const string IconCatalogsField = "_iconCatalogs";

		private const string ScaleModeField = "_scaleMode";
		private const string ScaleField = "_scale";
		private const string SortingOrderField = "_sortingOrder";
		private const string OpenOnStartField = "_openOnStart";
		private const string PanelSettingsField = "_panelSettings";
		private const string OpenField = "_open";

		private const string ButtonEnabledField = "_buttonEnabled";
		private const string ButtonClicksField = "_buttonClicks";
		private const string MultiClickWindowField = "_multiClickWindow";
		private const string ButtonAnchorField = "_buttonAnchor";
		private const string ButtonOpacityField = "_buttonOpacity";
		private const string ShortcutsField = "_shortcuts";

		private const string LockField = "_lock";
		private const string LockModeField = "_mode";
		private const string UnlockScopeField = "_unlockScope";
		private const string SkipInEditorField = "_skipInEditor";
		private const string MaxAttemptsField = "_maxAttempts";
		private const string CooldownSecondsField = "_cooldownSeconds";
		private const string SecretHashField = "_secretHash";
		private const string SecretSaltField = "_secretSalt";
		private const string PinLengthField = "_pinLength";

		private const float SecretButtonWidth = 60.0f;

		private static readonly GUIContent _buttonEnabledLabel = new GUIContent(
			"Open Button Enabled",
			"Shows the floating button that opens the panel. Off, a shortcut or code is the only way in.");

		private static readonly GUIContent _removeShortcutContent = new GUIContent("×", "Remove the shortcut.");
		private static readonly GUIContent _addShortcutContent = new GUIContent("Add Shortcut", "Add a key or a chord that toggles the panel.");
		private static readonly GUIContent _setSecretContent = new GUIContent("Set", "Replace the stored hash with one of the value typed here.");
		private static readonly GUIContent _clearSecretContent = new GUIContent("Clear", "Forget the stored hash, so the panel opens without asking.");

		private static SerializedObject _serialized;
		private static string _secretInput;
		private static string _secretError;

		[SettingsProvider]
		public static SettingsProvider Create()
		{
			return new SettingsProvider(SettingsPath, SettingsScope.Project)
			{
				label = "Panel",
				activateHandler = (_, _) => Release(),
				deactivateHandler = Release,
				guiHandler = _ => Draw(),
				keywords = new HashSet<string>
				{
					"omni",
					"debugger",
					"panel",
					"cheat",
					"button",
					"enabled",
					"clicks",
					"anchor",
					"position",
					"opacity",
					"shortcut",
					"hotkey",
					"binding",
					"scale",
					"sorting",
					"theme",
					"icon",
					"catalog",
					"lock",
					"pin",
					"password",
					"passcode",
				},
			};
		}

		private static void Draw()
		{
			OmniDebuggerProjectSettings settings = OmniDebuggerProjectSettings.instance;

			if (_serialized == null || _serialized.targetObject != settings)
			{
				_serialized = settings.CreateSerializedObject();
			}

			_serialized.Update();

			EditorGUIUtility.labelWidth = 220f;
			EditorGUILayout.Space();

			EditorGUILayout.HelpBox(
				"These options are what new OmniDebugger() and OmniDebuggerOptions.Default read. In play mode, edits " +
				"made here apply at once to a debugger built without options in code and to its panel; a player " +
				"reads the copy taken when it was built, and " +
				$"only a build with {OmniDebuggerDefines.Symbol} on carries them and the assets they reference. " +
				"Options passed to the debugger in code replace them as a whole.",
				MessageType.Info);

			SerializedProperty options = _serialized.FindProperty(OmniDebuggerProjectSettings.OptionsField);
			SerializedProperty panel = options.FindPropertyRelative(PanelField);
			SerializedProperty open = panel.FindPropertyRelative(OpenField);

			DrawHeader("Startup");

			using (new EditorGUI.IndentLevelScope())
			{
				DrawField(options, CreatePanelField);
				DrawField(panel, OpenOnStartField);
			}

			DrawHeader("Scaling and Layering");

			using (new EditorGUI.IndentLevelScope())
			{
				DrawField(panel, ScaleModeField);
				DrawField(panel, ScaleField);
				DrawField(panel, SortingOrderField);
				DrawField(panel, PanelSettingsField);
			}

			DrawHeader("Open Button");

			using (new EditorGUI.IndentLevelScope())
			{
				SerializedProperty buttonEnabled = open.FindPropertyRelative(ButtonEnabledField);
				EditorGUILayout.PropertyField(buttonEnabled, _buttonEnabledLabel);

				using (new EditorGUI.DisabledScope(!buttonEnabled.boolValue))
				{
					DrawField(open, ButtonClicksField);
					DrawField(open, MultiClickWindowField);
					DrawField(open, ButtonAnchorField);
					DrawField(open, ButtonOpacityField);
				}
			}

			DrawHeader("Shortcuts");

			using (new EditorGUI.IndentLevelScope())
			{
				DrawShortcuts(open.FindPropertyRelative(ShortcutsField));
			}

			DrawHeader("Lock");

			using (new EditorGUI.IndentLevelScope())
			{
				DrawLock(panel.FindPropertyRelative(LockField));
			}

			DrawHeader("Themes");

			using (new EditorGUI.IndentLevelScope())
			{
				DrawField(options, DefaultThemeField);
				DrawField(options, ThemesField);

				EditorGUILayout.HelpBox(
					"Nothing set, the panel switches between the built-in dark and light themes with one button. " +
					"A default theme alone replaces both and hides the switcher. Listed themes are offered next " +
					"to the built-in ones in a dropdown, starting from the default theme.",
					MessageType.Info);
			}

			DrawHeader("Icons");

			using (new EditorGUI.IndentLevelScope())
			{
				DrawField(options, IconCatalogsField);
			}

			if (_serialized.ApplyModifiedPropertiesWithoutUndo())
			{
				settings.Persist();
			}

			EditorGUILayout.Space();

			if (GUILayout.Button("Reset to Defaults", GUILayout.Width(140f)) &&
				EditorUtility.DisplayDialog(
					"Reset OmniDebugger Panel Settings",
					"Every option on this page goes back to the package defaults.",
					"Reset",
					"Cancel"))
			{
				settings.ResetToDefaults();
				_serialized.Update();
			}
		}

		private static void Release()
		{
			ShortcutDrawer.StopRecording();
			_serialized = null;
			_secretInput = null;
			_secretError = null;
		}

		private static void DrawShortcuts(SerializedProperty shortcuts)
		{
			if (shortcuts.arraySize == 0)
			{
				EditorGUILayout.LabelField("None: the button and code are the only ways in.", EditorStyles.miniLabel);
			}

			int removed = -1;

			for (int i = 0; i < shortcuts.arraySize; i++)
			{
				Rect row = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect());
				Rect field = new Rect(row.x, row.y, row.width - ShortcutDrawer.SideButtonWidth - ShortcutDrawer.Spacing, row.height);
				Rect remove = new Rect(field.xMax + ShortcutDrawer.Spacing, row.y, ShortcutDrawer.SideButtonWidth, row.height);

				ShortcutDrawer.DrawBinding(field, shortcuts.GetArrayElementAtIndex(i));

				if (GUI.Button(remove, _removeShortcutContent, EditorStyles.miniButton))
				{
					removed = i;
				}
			}

			if (removed >= 0)
			{
				ShortcutDrawer.StopRecording();
				shortcuts.DeleteArrayElementAtIndex(removed);
			}

			Rect add = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect());
			add.width = Mathf.Min(add.width, 120.0f);

			if (GUI.Button(add, _addShortcutContent))
			{
				int index = shortcuts.arraySize;
				shortcuts.arraySize = index + 1;
				shortcuts.GetArrayElementAtIndex(index).FindPropertyRelative(ShortcutDrawer.KeysField).arraySize = 0;
			}
		}

		private static void DrawLock(SerializedProperty lockOptions)
		{
			SerializedProperty mode = lockOptions.FindPropertyRelative(LockModeField);
			SerializedProperty hash = lockOptions.FindPropertyRelative(SecretHashField);
			SerializedProperty salt = lockOptions.FindPropertyRelative(SecretSaltField);
			SerializedProperty pinLength = lockOptions.FindPropertyRelative(PinLengthField);

			OmniDebuggerLockMode previous = (OmniDebuggerLockMode)mode.intValue;

			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField(mode);

			OmniDebuggerLockMode current = (OmniDebuggerLockMode)mode.intValue;

			if (EditorGUI.EndChangeCheck() &&
				previous != OmniDebuggerLockMode.None &&
				current != OmniDebuggerLockMode.None &&
				previous != current)
			{
				ClearSecret(hash, salt, pinLength);
			}

			if (current == OmniDebuggerLockMode.None)
			{
				EditorGUILayout.LabelField("Off: the panel opens without asking.", EditorStyles.miniLabel);
				return;
			}

			bool pin = current == OmniDebuggerLockMode.Pin;
			string noun = pin ? "PIN" : "password";
			bool hasSecret = !string.IsNullOrEmpty(hash.stringValue) && !string.IsNullOrEmpty(salt.stringValue);

			if (hasSecret)
			{
				EditorGUILayout.LabelField(" ", $"A {noun} is set. Only its salted hash is stored.", EditorStyles.miniLabel);
			}
			else
			{
				EditorGUILayout.HelpBox($"No {noun} is set, so the panel opens without asking.", MessageType.Warning);
			}

			DrawSecretInput(hash, salt, pinLength, pin, noun, hasSecret);

			if (!string.IsNullOrEmpty(_secretError))
			{
				EditorGUILayout.HelpBox(_secretError, MessageType.Error);
			}

			DrawField(lockOptions, UnlockScopeField);
			DrawField(lockOptions, SkipInEditorField);

			SerializedProperty maxAttempts = lockOptions.FindPropertyRelative(MaxAttemptsField);
			EditorGUILayout.PropertyField(maxAttempts);

			using (new EditorGUI.DisabledScope(maxAttempts.intValue <= 0))
			{
				DrawField(lockOptions, CooldownSecondsField);
			}

			EditorGUILayout.HelpBox(
				"Only the runtime panel asks; the editor window never does. The hash ships with the build, so " +
				"this keeps testers and players out by accident, not a determined attacker: a short PIN is " +
				"quick to guess from it.",
				MessageType.Info);
		}

		private static void DrawSecretInput(
			SerializedProperty hash,
			SerializedProperty salt,
			SerializedProperty pinLength,
			bool pin,
			string noun,
			bool hasSecret)
		{
			Rect row = EditorGUILayout.GetControlRect();
			float buttons = (SecretButtonWidth + ShortcutDrawer.Spacing) * 2.0f;
			Rect field = new Rect(row.x, row.y, row.width - buttons, row.height);
			Rect set = new Rect(field.xMax + ShortcutDrawer.Spacing, row.y, SecretButtonWidth, row.height);
			Rect clear = new Rect(set.xMax + ShortcutDrawer.Spacing, row.y, SecretButtonWidth, row.height);

			GUIContent label = new GUIContent(
				pin ? "New PIN" : "New Password",
				pin
					? $"{OmniDebuggerLockOptions.MinPinLength} to {OmniDebuggerLockOptions.MaxPinLength} digits."
					: "Any characters.");

			_secretInput = EditorGUI.PasswordField(field, label, _secretInput ?? string.Empty);

			if (GUI.Button(set, _setSecretContent, EditorStyles.miniButton))
			{
				SetSecret(hash, salt, pinLength, pin, noun);
			}

			using (new EditorGUI.DisabledScope(!hasSecret))
			{
				if (GUI.Button(clear, _clearSecretContent, EditorStyles.miniButton))
				{
					ClearSecret(hash, salt, pinLength);
				}
			}
		}

		private static void SetSecret(
			SerializedProperty hash,
			SerializedProperty salt,
			SerializedProperty pinLength,
			bool pin,
			string noun)
		{
			string secret = _secretInput ?? string.Empty;

			if (secret.Length == 0)
			{
				_secretError = $"Type a {noun} first.";
				return;
			}

			if (pin && !LockSecret.IsValidPin(secret))
			{
				_secretError =
					$"A PIN takes {OmniDebuggerLockOptions.MinPinLength} to {OmniDebuggerLockOptions.MaxPinLength} digits and nothing else.";
				return;
			}

			string newSalt = LockSecret.CreateSalt();
			salt.stringValue = newSalt;
			hash.stringValue = LockSecret.Hash(secret, newSalt);
			pinLength.intValue = pin ? secret.Length : 0;

			_secretInput = string.Empty;
			_secretError = null;
			GUI.FocusControl(null);
		}

		private static void ClearSecret(SerializedProperty hash, SerializedProperty salt, SerializedProperty pinLength)
		{
			hash.stringValue = string.Empty;
			salt.stringValue = string.Empty;
			pinLength.intValue = 0;
			_secretInput = string.Empty;
			_secretError = null;
			GUI.FocusControl(null);
		}

		private static void DrawHeader(string title)
		{
			EditorGUILayout.Space();
			EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
		}

		private static void DrawField(SerializedProperty parent, string field) =>
			EditorGUILayout.PropertyField(parent.FindPropertyRelative(field), includeChildren: true);
	}
}
#endif