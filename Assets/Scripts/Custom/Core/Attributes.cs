
using System;
using UnityEngine;

namespace Custom
{
    // Field Attribute To Be Shown In Specific Generation Mode
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class FlagAttribute : Attribute 
    {
        public int flag;
        public FlagAttribute(int flag){ this.flag = flag;}
    }
}