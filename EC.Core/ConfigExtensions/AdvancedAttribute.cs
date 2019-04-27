﻿using System;

namespace EC.Core.ConfigExtensions
{
    public class AdvancedAttribute : Attribute
    {
        public AdvancedAttribute(bool isAdvanced)
        {
            IsAdvanced = isAdvanced;
        }

        public bool IsAdvanced { get; }
    }
}