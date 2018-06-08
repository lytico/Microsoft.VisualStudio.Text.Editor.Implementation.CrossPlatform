//
// DispaherTimer.cs
//
// Author:
//       David Karlaš <david.karlas@microsoft.com>
//
// Copyright (c) 2017 Microsoft Corp
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Timers;

namespace Microsoft.VisualStudio.Text.Editor
{
	public class DispatcherTimer
    {
        public DispatcherTimer()
        {
        }

        Timer timer;
       //Foundation.NSObject obj = new Foundation.NSObject();
        public DispatcherTimer(TimeSpan interval, EventHandler callback)
        {
            timer = new Timer(interval.TotalMilliseconds);
            timer.Elapsed += elapsed;
            timer.Start();
            Tick += callback;
        }

        private void elapsed(object sender, ElapsedEventArgs e)
        {
            //obj.BeginInvokeOnMainThread(() => Tick?.Invoke(this, EventArgs.Empty));
        }

        public bool IsEnabled
        {
            get
            {
                return timer.Enabled;
            }
            set
            {
                timer.Enabled = value;
            }
        }

        public TimeSpan Interval
        {
            get
            {
                return TimeSpan.FromMilliseconds(timer.Interval);
            }

            set
            {
                timer.Interval = value.TotalMilliseconds;
            }
        }

        public void Start()
        {
            timer.Start();
        }

        public void Stop()
        {
            timer.Stop();
        }
        public event EventHandler Tick;
        public object Tag { get; set; }
    }
}
