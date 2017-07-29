using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Jamify
{
    class GifManagerService : IDisposable
    {
        public BitmapSource[] Frames { get; private set; }

        private Thread FrameUpdateThread { get; set; }
        private Stopwatch BeatStopwatch { get; }

        public double RenderDelay { get; set; }
        private double RenderDuration { get; set; }
        private bool Running { get; set; }
        private IList<double> BeatSamples { get; }
        private double LastFrameElapsed { get; set; }


        #region Gif playback parameters
        private int _startFrame;
        public int StartFrame
        {
            get => _startFrame;
            set
            {
                var changed = _startFrame != value;
                if (changed)
                {
                    _startFrame = value;
                    Sync();
                }
            }
        }

        private int _endFrame;
        public int EndFrame
        {
            get => _endFrame;
            set
            {
                var changed = _endFrame != value;
                if (changed)
                {
                    _endFrame = value;
                    Sync();
                }
            }
        }

        private bool _reverse;
        public bool Reverse
        {
            get => _reverse;
            set
            {
                var changed = _reverse != value;
                if (changed)
                {
                    _reverse = value;
                    Sync();
                }
            }
        }

        private int PlayingFrameCount { get; set; }
        public int CurrentFrameIndex { get; private set; }
        public int Offset { get; set; }
        public double LoopDuration { get; set; }
        private double FrameDuration { get; set; }
        #endregion #region Gif playback parameters

        public event EventHandler FrameUpdate;

        public GifManagerService()
        {
            RenderDelay = 1000/60d;

            BeatStopwatch = new Stopwatch();
            BeatSamples = new List<double>();
        }

        public void Start(string fileName)
        {
            Frames = GenerateFrames(fileName);
            Clear();
            Stop();
            Running = true;
            //FrameUpdateTimer.Start();
            FrameUpdateThread = new Thread(FrameUpdateLoop);
            FrameUpdateThread.IsBackground = true;
            FrameUpdateThread.Start();
        }

        private void Sync()
        {
            PlayingFrameCount = EndFrame - StartFrame + 1;
            if (Reverse)
            {
                PlayingFrameCount += PlayingFrameCount - 2;
            }
        }


        public void Clear()
        {
            BeatStopwatch.Reset();
            BeatSamples.Clear();
            Offset = 0;
            LoopDuration = 500;
            StartFrame = 0;
            EndFrame = Frames.Length - 1;
            Reverse = false;
            Sync();
        }

        public void Stop()
        {
            if (!Running)
            {
                return;
            }
            Clear();
            Running = false;
            FrameUpdateThread.Join();
        }

        public void FrameUpdateLoop()
        {
            while (Running)
            {
                Thread.Sleep((int)RenderDelay);
                DoFrameUpdating();
            }
        }

        private void DoFrameUpdating()
        {
            var beginningRender = BeatStopwatch.Elapsed.TotalMilliseconds;
            var noRevFrameCount = EndFrame - StartFrame + 1;
            if (PlayingFrameCount == 1)
            {
                FrameDuration = 0;
                CurrentFrameIndex = StartFrame;
            }
            else
            {
                FrameDuration = LoopDuration / PlayingFrameCount;
                CurrentFrameIndex = (((int)(BeatStopwatch.Elapsed.TotalMilliseconds / FrameDuration) + Offset) % PlayingFrameCount) + StartFrame;
                if (CurrentFrameIndex >= (StartFrame + noRevFrameCount))
                {
                    CurrentFrameIndex = PlayingFrameCount - CurrentFrameIndex;
                }
            }
            FrameUpdate?.Invoke(this, EventArgs.Empty);
            var endingRender = BeatStopwatch.Elapsed.TotalMilliseconds;
            RenderDuration = endingRender - beginningRender;
            LastFrameElapsed = endingRender;
        }

        public BitmapSource[] GenerateFrames(string filePath)
        {
            using (Stream imageStreamSource = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var decoder = new GifBitmapDecoder(imageStreamSource, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                var frames = new BitmapSource[decoder.Frames.Count];
                var firstFrame = decoder.Frames[0];
                var width = firstFrame.PixelWidth;
                var height = firstFrame.PixelHeight;
                var bmp = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
                for (var i = 0; i < decoder.Frames.Count; i++)
                {
                    var drawingVisual = new DrawingVisual();
                    using (var drawingContext = drawingVisual.RenderOpen())
                    {
                        drawingContext.DrawImage(decoder.Frames[i], new Rect(0, 0, width, height));
                    }
                    bmp.Render(drawingVisual);
                    frames[i] = bmp.CloneCurrentValue();
                }
                return frames;
            }

        }

        public void Beat()
        {
            if (BeatStopwatch.IsRunning)
            {
                BeatSamples.Add(BeatStopwatch.Elapsed.TotalMilliseconds);
                LoopDuration = BeatSamples.Average();
                BeatStopwatch.Restart();
            }
            else
            {
                BeatStopwatch.Start();
            }
        }

        public string GetStatusString()
        {
            var elapsedTime = BeatStopwatch.Elapsed.TotalMilliseconds - LastFrameElapsed;

            var bpm = 60000/LoopDuration;
            var renderFps = 1000/elapsedTime;
            var str = 
                      "Beat duration:            " + LoopDuration.ToString("0.00") + " ms \n" +
                      "Beats pear minute:        " + bpm.ToString("0.00") + " bpm\n" +
                      "Beat sample size:         " + BeatSamples.Count + " \n" +
                      "Frames in image:          " + Frames.Length + " f.\n" +
                      "Frames playing:           " + PlayingFrameCount + " f.\n" +
                      "Start frame:              " + (StartFrame + 1) + "\n" +
                      "End frame:                " + (EndFrame + 1) + "\n" +
                      "Current frame:            " + (CurrentFrameIndex + 1) + "\n" +
                      (Reverse ? "Reversing" : "Not reversing") + " gif\n" +
                      "Frame offset:             " + Offset + " f.\n" +
                      "Gif frame duration:       " + FrameDuration.ToString("0.00") + " ms\n" +
                      "Gif frames per second:    " + (FrameDuration > 0 ? FrameDuration.ToString("0.00") : "-") + " fps\n" +
                      "Render delay:             " + RenderDelay.ToString("0.00") + " ms\n" +
                      "Actual render delay:      " + elapsedTime.ToString("0.00") + " ms\n" +
                      "Render frames per second: " + (BeatStopwatch.IsRunning ? renderFps.ToString("0.00") : "-") + " fps\n" +
                      "Render duration:          " + RenderDuration.ToString("0.00") + " ms\n";
            return str;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
