using System;
using System.Windows.Media.Imaging;

namespace Jamify
{
    class FrameChangedEventArgs : EventArgs
    {
        public FrameChangedEventArgs(bool frameChanged, BitmapSource currentFrame)
        {
            FrameChanged = frameChanged;
            CurrentFrame = currentFrame;
        }

        public bool FrameChanged { get; }
        public BitmapSource CurrentFrame { get; }
    }
}