using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Squid;
using Spark;
using System.Reflection;
using System.ComponentModel;
using SharpDX;

using Point = Squid.Point;
using SharpDX.Direct3D11;
using System.Collections;

namespace Spark.Client
{
    public class PropertyControl : Frame
    {
        protected static readonly float Interval = 0.15f;

        protected GUIProperty property;
        protected float Timer;

        public int? RowHeight;
        public bool Expandable;

        public PropertyControl(GUIProperty property)
        {
            this.property = property;
            this.Timer = Interval;
        }

        protected void NotifyChange()
        {
            //if (OnMemberChanged != null)
           //     OnMemberChanged(ProtocolEntry);

           // ProtocolEntry = new ProtocolMemberValue(Instance, Property.Name);
        }
    }

}
