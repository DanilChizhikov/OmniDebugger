using System;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class PanelNavigation : IDisposable
	{
		private readonly VisualElement _root;
		private readonly VisualElement _panel;
		private readonly PopupLayer _popups;
		private readonly Action _close;
		private readonly IVisualElementScheduledItem _focusOnOpen;
		private readonly FocusRing _ring;

		private VisualElement _lastFocused;
		private bool _disposed;

		public PanelNavigation(VisualElement root, VisualElement panel, PopupLayer popups, Action close)
		{
			_root = root ?? throw new ArgumentNullException(nameof(root));
			_panel = panel ?? throw new ArgumentNullException(nameof(panel));
			_popups = popups ?? throw new ArgumentNullException(nameof(popups));
			_close = close;

			_focusOnOpen = _root.schedule.Execute(FocusPanel);
			_focusOnOpen.Pause();
			_ring = new FocusRing(_root);

			_root.RegisterCallback<NavigationMoveEvent>(OnNavigationMove, TrickleDown.TrickleDown);
			_root.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
			_root.RegisterCallback<FocusInEvent>(OnFocusIn, TrickleDown.TrickleDown);
			_root.RegisterCallback<NavigationCancelEvent>(OnNavigationCancel);
		}

		public static bool IsTextInput(VisualElement element)
		{
			for (VisualElement current = element; current != null; current = current.parent)
			{
				if (current.ClassListContains(TextInputBaseField<string>.ussClassName))
				{
					return true;
				}
			}

			return false;
		}

		public static VisualElement FindFocusable(VisualElement element)
		{
			if (element.resolvedStyle.display == DisplayStyle.None ||
				element.ClassListContains(TextInputBaseField<string>.ussClassName))
			{
				return null;
			}

			if (element.canGrabFocus)
			{
				return element;
			}

			for (int i = 0; i < element.hierarchy.childCount; i++)
			{
				VisualElement found = FindFocusable(element.hierarchy[i]);

				if (found != null)
				{
					return found;
				}
			}

			return null;
		}

		public void FocusOnOpen() => _focusOnOpen.ExecuteLater(0);

		public void Release()
		{
			_focusOnOpen.Pause();
			SetNavigating(false);

			if (_root.panel?.focusController?.focusedElement is VisualElement focused && _root.Contains(focused))
			{
				focused.Blur();
			}
		}

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;
			Release();

			_root.UnregisterCallback<NavigationMoveEvent>(OnNavigationMove, TrickleDown.TrickleDown);
			_root.UnregisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
			_root.UnregisterCallback<FocusInEvent>(OnFocusIn, TrickleDown.TrickleDown);
			_root.UnregisterCallback<NavigationCancelEvent>(OnNavigationCancel);
			_ring.Dispose();
			_lastFocused = null;
		}

		private void FocusPanel()
		{
			if (_popups.IsShowing)
			{
				return;
			}

			VisualElement target = CanFocus(_lastFocused) ? _lastFocused : null;

			if (target == null)
			{
				VisualElement tab = _panel.Q(className: OmniDebuggerUiClasses.TabSelected);
				target = CanFocus(tab) ? tab : FindFocusable(_panel);
			}

			target?.Focus();
		}

		private void SetNavigating(bool navigating)
		{
			_root.EnableInClassList(OmniDebuggerUiClasses.RootNavigating, navigating);
			_ring.SetActive(navigating);
		}

		private bool CanFocus(VisualElement element)
		{
			if (element == null || element.panel == null || !element.canGrabFocus || !_panel.Contains(element))
			{
				return false;
			}

			for (VisualElement current = element; current != _panel; current = current.parent)
			{
				if (current.resolvedStyle.display == DisplayStyle.None)
				{
					return false;
				}
			}

			return true;
		}

		private void OnNavigationMove(NavigationMoveEvent evt)
		{
			if (_root.ClassListContains(OmniDebuggerUiClasses.RootNavigating))
			{
				return;
			}

			SetNavigating(true);

			// The first move only reveals where the focus already is, so it never jumps unseen.
			if (evt.target is VisualElement target && target != _root && _root.Contains(target))
			{
				_root.panel?.focusController?.IgnoreEvent(evt);
				evt.StopPropagation();
			}
		}

		private void OnPointerDown(PointerDownEvent evt) => SetNavigating(false);

		private void OnFocusIn(FocusInEvent evt)
		{
			if (evt.target is VisualElement target && _panel.Contains(target) && !IsTextInput(target))
			{
				_lastFocused = target;
			}
		}

		private void OnNavigationCancel(NavigationCancelEvent evt)
		{
			if (evt.target is VisualElement target && IsTextInput(target))
			{
				return;
			}

			if (_popups.IsShowing)
			{
				_popups.Hide();
				evt.StopPropagation();
				return;
			}

			if (_close != null)
			{
				_close();
				evt.StopPropagation();
			}
		}
	}
}
