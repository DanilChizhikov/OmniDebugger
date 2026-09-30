#if OMNI_DEBUGGER
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DTech.OmniDebugger.UI.Editor
{
	[FilePath("DTech/OmniDebugger/EditorWindow.asset", FilePathAttribute.Location.PreferencesFolder)]
	internal sealed class EditorWindowPrefs : ScriptableSingleton<EditorWindowPrefs>
	{
		public const float DefaultSplitWidth = 260.0f;
		public const float MinSplitWidth = 120.0f;

		public EditorWindowMode Mode
		{
			get => _mode;
			set => Set(ref _mode, value);
		}

		public float SplitWidth
		{
			get => _splitWidth >= MinSplitWidth ? _splitWidth : DefaultSplitWidth;
			set => Set(ref _splitWidth, value);
		}

		public string SelectedPath
		{
			get => _selectedPath;
			set => Set(ref _selectedPath, value);
		}

		public string Arguments
		{
			get => _arguments;
			set => Set(ref _arguments, value);
		}

		[SerializeField] private EditorWindowMode _mode = EditorWindowMode.Commands;
		[SerializeField] private float _splitWidth = DefaultSplitWidth;
		[SerializeField] private string _selectedPath;
		[SerializeField] private List<string> _expandedPaths = new ();
		[SerializeField] private List<string> _collapsedSections = new ();
		[SerializeField] private string _arguments;

		private bool _saveQueued;

		public bool IsExpanded(string path) => _expandedPaths.Contains(path);

		public void SetExpanded(string path, bool expanded) => SetMember(_expandedPaths, path, expanded);

		public bool IsSectionCollapsed(string title) => _collapsedSections.Contains(title);

		public void SetSectionCollapsed(string title, bool collapsed) => SetMember(_collapsedSections, title, collapsed);

		public void Reset()
		{
			_mode = EditorWindowMode.Commands;
			_splitWidth = DefaultSplitWidth;
			_selectedPath = null;
			_expandedPaths.Clear();
			_collapsedSections.Clear();
			_arguments = null;
			QueueSave();
		}

		private void SetMember(List<string> list, string item, bool member)
		{
			if (string.IsNullOrEmpty(item) || list.Contains(item) == member)
			{
				return;
			}

			if (member)
			{
				list.Add(item);
			}
			else
			{
				list.Remove(item);
			}

			QueueSave();
		}

		private void Set<T>(ref T field, T value)
		{
			if (EqualityComparer<T>.Default.Equals(field, value))
			{
				return;
			}

			field = value;
			QueueSave();
		}

		private void QueueSave()
		{
			if (_saveQueued)
			{
				return;
			}

			_saveQueued = true;
			EditorApplication.delayCall += () =>
			{
				_saveQueued = false;
				Save(true);
			};
		}
	}
}
#endif
