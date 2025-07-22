using System.Collections.Generic;

namespace Myra.Graphics2D.UI
{
	internal enum InputEventType
	{
		MouseLeft,
		MouseEntered,
		MouseMoved,
		MouseWheel,
		TouchLeft,
		TouchEntered,
		TouchMoved,
		TouchDown,
		TouchUp,
		TouchDoubleClick,
        MouseClick,
    }

	internal interface IInputEventsProcessor
	{
		void ProcessEvent(InputEventType eventType);
	}

	internal static class InputEventsManager
	{
		private struct InputEvent
		{
			public IInputEventsProcessor Processor;
			public InputEventType Type;

			public InputEvent(IInputEventsProcessor processor, InputEventType type)
			{
				Processor = processor;
				Type = type;
			}
		}

		private static readonly Stack<InputEvent> _events = new Stack<InputEvent>();

		public static void Queue(IInputEventsProcessor processor, InputEventType type)
		{
			_events.Push(new InputEvent(processor, type));
		}

		public static void ProcessEvents()
		{
			while(_events.Count > 0)
			{
				var ev = _events.Pop();

				ev.Processor.ProcessEvent(ev.Type);
			}
		}
	}
}
