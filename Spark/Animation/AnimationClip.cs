using System;
using System.Collections.Generic;
using SharpDX;
using System.Collections.ObjectModel;

namespace Spark
{
    public class AnimationClip : Asset
    {
        private float _durationInTicks;
        private float _ticksPerSecond;

        public readonly ReadOnlyCollection<AnimationChannel> Channels;
        private List<AnimationChannel> _channels = new List<AnimationChannel>();

        public float Duration
        {
            get { return _durationInTicks; }
            set
            {
                _durationInTicks = value;
                DurationSeconds = _durationInTicks / _ticksPerSecond;
                DurationSecondsInverse = 1f / DurationSeconds;
            }
        }

        public float DurationSeconds { get; private set; }
        public float DurationSecondsInverse { get; private set; }

        public float TicksPerSecond
        {
            get { return _ticksPerSecond; }
            set
            {
                _ticksPerSecond = value;
                DurationSeconds = _durationInTicks / _ticksPerSecond;
                DurationSecondsInverse = 1f / DurationSeconds;
            }
        }


        public AnimationClip()
        {
            Channels = new ReadOnlyCollection<AnimationChannel>(_channels);
        }

        public void AddChannel(AnimationChannel channel)
        {
            _channels.Add(channel);
        }

        public void RemoveChannel(AnimationChannel channel)
        {
            _channels.Remove(channel);
        }

        public void GetBonePose(Bone bone, float time, ref BonePose result)
        {
            //float ticks = time * TicksPerSecond;
            //float delta = ticks % Duration;

            //if (rebuildLookup)
            //    RebuildLookupTable();

            if (time < DurationSeconds)
                time = (time * TicksPerSecond) % Duration;
            else
                time = DurationSeconds;

            int channelCount = Channels.Count;
            for (int i = 0; i < channelCount; i++)
            {
                if (Channels[i].Hash == bone.Hash)
                {
                    Channels[i].GetBoneTransform(bone, time, ref result);
                    return;
                }
            }
        }


        public AnimationClip CreateRange(string name, float start, float end)
        {
            AnimationClip clip = new AnimationClip
            {
                Duration = end - start,
                Name = name,
                TicksPerSecond = TicksPerSecond
            };

            foreach (AnimationChannel channel in Channels)
            {
                AnimationChannel copy = channel.CopyRange(start, end);

                if (copy.Position.Count > 0 || copy.Scale.Count > 0 || copy.Rotation.Count > 0)
                    clip.AddChannel(copy);
            }

            return clip;
        }
    }

}
