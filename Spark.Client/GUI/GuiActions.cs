using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Squid;

namespace Spark.Client
{
    public class Resize : GuiAction
    {
        private Point size;
        private float duration;
        private float time;
        private Point begin;
        private Point change;
        private Point current;
        private AnchorStyles anchor = AnchorStyles.Bottom | AnchorStyles.Right;

        public Resize(Point size, float duration)
        {
            this.size = size;
            this.duration = duration;
        }

        public Resize(Point size, float duration, AnchorStyles anchor)
        {
            this.anchor = anchor;
            this.size = size;
            this.duration = duration;
        }

        public override void Start()
        {
            begin = Control.Size;
            change = size - begin;
        }

        public override void Update(float dt)
        {
            time += Gui.TimeElapsed;
            current = Easing.EaseInOut(time, begin, change, duration);
            Control.ResizeTo(current, anchor);

            if (time >= duration)
            {
                Control.Size = size;
                IsFinished = true;
            }
        }
    }

    public class Move : GuiAction
    {
        private Point position;
        private float duration;
        private float time;
        private Point begin;
        private Point change;

        public Move(Point position, float duration)
        {
            this.position = position;
            this.duration = duration;
        }

        public override void Start()
        {
            begin = Control.Position;
            change = position - begin;
        }

        public override void Update(float dt)
        {
            time += Gui.TimeElapsed;
            Control.Position = Easing.EaseInOut(time, begin, change, duration);

            if (time >= duration)
            {
                Control.Position = position;
                IsFinished = true;
            }
        }
    }

    public class Fade : GuiAction
    {
        public float opacity;
        public float duration;

        private float time;
        private float begin;
        private float change;

        public Fade() { }

        public Fade(float opacity, float duration)
        {
            this.opacity = opacity;
            this.duration = duration;
        }

        public override void Start()
        {
            begin = Control.Opacity;
            change = opacity - begin;
        }

        public override void Update(float dt)
        {
            time += Gui.TimeElapsed;
            Control.Opacity = Easing.EaseInOut(time, begin, change, duration);

            if (time >= duration)
            {
                Control.Opacity = opacity;
                IsFinished = true;
            }
        }
    }
}
