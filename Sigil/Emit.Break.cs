﻿using System.Reflection.Emit;

namespace Sigil
{
    public partial class Emit<DelegateType>
    {
        /// <summary>
        /// Emits a break instruction for use with a debugger.
        /// </summary>
        public Emit<DelegateType> Break()
        {
            UpdateState(OpCodes.Break);

            return this;
        }
    }
}
