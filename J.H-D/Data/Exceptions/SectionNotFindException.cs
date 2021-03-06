﻿using System;

namespace J.H_D.Data.Exceptions
{
    public class SectionNotFoundException : Exception
    {
        public SectionNotFoundException()
        {

        }

        public SectionNotFoundException(string Message)
            : base(Message)
        {

        }

        public SectionNotFoundException(string Message, Exception Inner)
            : base(Message, Inner)
        {

        }
    }
}
