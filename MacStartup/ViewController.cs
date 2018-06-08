using System;

using AppKit;
using Foundation;
using Microsoft.VisualStudio.Text.Editor;

namespace MacStartup
{
    public partial class ViewController : NSViewController
    {
        public ViewController(IntPtr handle) : base(handle)
        {
        }
        public override void LoadView()
        {
            base.LoadView();
            var view = new SkiaTextViewHost(true, PlatformCatalog.Instance.TextEditorFactoryService);
            AddChildView(view);
        }

        private void AddChildView(NSView childView)
        {
            childView.Bounds = View.Bounds;
            this.View.AddSubview(childView);

            childView.TranslatesAutoresizingMaskIntoConstraints = false;

            childView.TopAnchor.ConstraintEqualToAnchor(View.TopAnchor).Active = true;
            childView.LeadingAnchor.ConstraintEqualToAnchor(View.LeadingAnchor).Active = true;
            childView.TrailingAnchor.ConstraintEqualToAnchor(View.TrailingAnchor).Active = true;
            childView.BottomAnchor.ConstraintEqualToAnchor(View.BottomAnchor).Active = true;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
        }

        public override NSObject RepresentedObject
        {
            get
            {
                return base.RepresentedObject;
            }
            set
            {
                base.RepresentedObject = value;
                // Update the view, if already loaded.
            }
        }
    }
}
