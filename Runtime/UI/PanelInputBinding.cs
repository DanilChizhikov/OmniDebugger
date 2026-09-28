#if OMNI_DEBUGGER_UGUI
using System;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace DTech.OmniDebugger.UI
{
	internal sealed class PanelInputBinding : IDisposable
	{
		private const string ObjectName = "OmniDebugger Panel Input";

		private readonly Transform _owner;

		private GameObject _inputObject;
		private PanelEventHandler _eventHandler;
		private PanelRaycaster _raycaster;

		public PanelInputBinding(Transform owner)
		{
			_owner = owner != null ? owner : throw new ArgumentNullException(nameof(owner));
		}

		public void Sync(IPanel panel)
		{
			if (panel is not IRuntimePanel runtimePanel)
			{
				return;
			}

			GameObject assigned = runtimePanel.selectableGameObject;

			if (_inputObject == null)
			{
				Adopt(assigned);
			}
			else if (assigned != null && assigned != _inputObject)
			{
				Object.Destroy(assigned);
			}

			if (_inputObject.transform.parent != _owner)
			{
				_inputObject.transform.SetParent(_owner, false);
			}

			if (_eventHandler.panel != panel)
			{
				_eventHandler.panel = panel;
			}

			if (_raycaster.panel != panel)
			{
				_raycaster.panel = panel;
			}

			if (!ReferenceEquals(runtimePanel.selectableGameObject, _inputObject))
			{
				runtimePanel.selectableGameObject = _inputObject;
			}
		}

		public void Release(IPanel panel)
		{
			if (_inputObject == null)
			{
				Clear();
				return;
			}

			if (panel is IRuntimePanel runtimePanel &&
				ReferenceEquals(runtimePanel.selectableGameObject, _inputObject))
			{
				runtimePanel.selectableGameObject = null;
			}

			Object.Destroy(_inputObject);
			Clear();
		}

		public void Dispose() => Release(null);

		private void Adopt(GameObject assigned)
		{
			_inputObject = assigned != null
				? assigned
				: new GameObject(ObjectName, typeof(PanelEventHandler), typeof(PanelRaycaster));

			_eventHandler = _inputObject.GetComponent<PanelEventHandler>();

			if (_eventHandler == null)
			{
				_eventHandler = _inputObject.AddComponent<PanelEventHandler>();
			}

			_raycaster = _inputObject.GetComponent<PanelRaycaster>();

			if (_raycaster == null)
			{
				_raycaster = _inputObject.AddComponent<PanelRaycaster>();
			}
		}

		private void Clear()
		{
			_inputObject = null;
			_eventHandler = null;
			_raycaster = null;
		}
	}
}
#endif