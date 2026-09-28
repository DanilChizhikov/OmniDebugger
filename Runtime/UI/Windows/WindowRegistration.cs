using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class WindowRegistration : IOmniDebuggerWindow
	{
		private readonly WindowRegistry _owner;

		public string Id { get; }

		public string Title { get; }

		public WindowKind Kind { get; }

		public Action<VisualElement> BuildContent { get; }

		public IReadOnlyList<string> CommandKeys { get; }

		public Vector2 Size { get; }

		public bool IsOpen => _isOpen;

		public bool IsCollapsed => _isCollapsed;

		public bool IsRegistered { get; set; } = true;

		private bool _isOpen;
		private bool _isCollapsed;

		public WindowRegistration(
			WindowRegistry owner,
			string id,
			string title,
			WindowKind kind,
			Action<VisualElement> buildContent,
			IReadOnlyList<string> commandKeys,
			Vector2 size,
			bool isOpen,
			bool isCollapsed = false)
		{
			_owner = owner;
			Id = id;
			Title = string.IsNullOrWhiteSpace(title) ? id : title;
			Kind = kind;
			BuildContent = buildContent;
			CommandKeys = commandKeys ?? Array.Empty<string>();
			Size = size;
			_isOpen = isOpen;
			_isCollapsed = isCollapsed;
		}

		public void Open() => SetOpen(true);

		public void Close() => SetOpen(false);

		public void SetCollapsed(bool collapsed)
		{
			if (_isCollapsed == collapsed)
			{
				return;
			}

			_isCollapsed = collapsed;
			_owner.NotifyChanged(this);
		}

		public void SetOpen(bool open)
		{
			if (_isOpen == open)
			{
				return;
			}

			_isOpen = open;
			_owner.NotifyChanged(this);
		}
	}
}