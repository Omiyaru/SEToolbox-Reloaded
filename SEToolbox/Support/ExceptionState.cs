using System;
using System.Globalization;
using System.Runtime.Serialization;
using SEToolbox.Converters;

namespace SEToolbox.Support
{
    [Serializable]
    public class ToolboxException : Exception
    {
        private readonly string _friendlyMessage;

        public ToolboxException(ExceptionState state, params object[] arguments)
        {
            EnumToResourceConverter converter = new();
            Arguments = arguments;
            _friendlyMessage = string.Format((string)converter.Convert(state, typeof(string), null, CultureInfo.CurrentUICulture), Arguments);
        }

        public override string Message
        {
            get => _friendlyMessage;
        }

        public object[] Arguments { get; private set; }

        protected ToolboxException(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }
    }
}
