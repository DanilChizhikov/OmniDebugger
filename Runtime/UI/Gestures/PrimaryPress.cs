using UnityEngine;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class PrimaryPress : IPointerEvent
	{
		private const int PrimaryButton = 0;
		private const int PrimaryButtonMask = 1;

		int IPointerEvent.pointerId => _source.pointerId;
		string IPointerEvent.pointerType => _source.pointerType;
		bool IPointerEvent.isPrimary => _source.isPrimary;
		int IPointerEvent.button => PrimaryButton;
		int IPointerEvent.pressedButtons => _source.pressedButtons | PrimaryButtonMask;
		Vector3 IPointerEvent.position => _source.position;
		Vector3 IPointerEvent.localPosition => _source.localPosition;
		Vector3 IPointerEvent.deltaPosition => _source.deltaPosition;
		float IPointerEvent.deltaTime => _source.deltaTime;
		int IPointerEvent.clickCount => 1;
		float IPointerEvent.pressure => _source.pressure;
		float IPointerEvent.tangentialPressure => _source.tangentialPressure;
		float IPointerEvent.altitudeAngle => _source.altitudeAngle;
		float IPointerEvent.azimuthAngle => _source.azimuthAngle;
		float IPointerEvent.twist => _source.twist;
		Vector2 IPointerEvent.tilt => _source.tilt;
		PenStatus IPointerEvent.penStatus => _source.penStatus;
		Vector2 IPointerEvent.radius => _source.radius;
		Vector2 IPointerEvent.radiusVariance => _source.radiusVariance;
		EventModifiers IPointerEvent.modifiers => _source.modifiers;
		bool IPointerEvent.shiftKey => _source.shiftKey;
		bool IPointerEvent.ctrlKey => _source.ctrlKey;
		bool IPointerEvent.commandKey => _source.commandKey;
		bool IPointerEvent.altKey => _source.altKey;
		bool IPointerEvent.actionKey => _source.actionKey;

		private IPointerEvent _source;

		public void Set(IPointerEvent source) => _source = source;
	}
}
