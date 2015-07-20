using System.Collections.Generic;
using Surge.Models.Cerebral;
using Surge.Models.System;

namespace Surge.Messages.Cerebral
{
    public class RegisterStreamMsg
    {
        public RegisterStreamMsg(StreamDefinition definition)
        {
            Definition = definition;
        }

        public StreamDefinition Definition { get; private set; }
    }
}
