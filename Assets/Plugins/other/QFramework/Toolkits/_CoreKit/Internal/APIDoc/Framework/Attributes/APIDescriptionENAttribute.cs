/****************************************************************************
 * Copyright (c) 2015 - 2022 liangxiegame UNDER MIT License
 *
 * http://qframework.cn
 * https://github.com/liangxiegame/QFramework
 * https://gitee.com/liangxiegame/QFramework
 ****************************************************************************/

using System;

namespace QFramework
{
    public class APIDescriptionENAttribute : Attribute
    {
        public APIDescriptionENAttribute(string description)
        {
            Description = description;
        }

        public string Description { get; private set; }
    }
}