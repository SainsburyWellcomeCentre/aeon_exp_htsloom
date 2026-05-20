using System;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Xml.Serialization;
using Bonsai;
using OpenCV.Net;

namespace HtsLoom.Extensions
{
    // OPTION 1 — inherits from Bonsai.Vision.VideoWriter.
    //
    // Pros: gets every VideoWriter / FileSink property (FourCC, FrameRate, FrameSize,
    //       ResizeInterpolation, Suffix, Buffered, Overwrite, FileName) for free; the .bonsai XML
    //       keeps inherited <cv:...> property elements unchanged when migrating from cv:VideoWriter.
    //       Bonsai.Vision.VideoWriter is a regular FileSink with a public Process(IObservable<IplImage>),
    //       so no expression-builder reflection is needed — we just add a second Process overload that
    //       Bonsai discovers alongside the inherited one and picks based on the input type.
    // Cons: the inherited Process(IObservable<IplImage>) still exists; if you accidentally wire raw
    //       IplImage data straight into this node Bonsai will use the inherited overload and fall back
    //       to the static FileName property.
    [Description(
        "Writes a sequence of images into a compressed AVI file. The file name and frame rate are " +
        "taken from Item2 and Item3 of the input Tuple<IplImage, string, double>, so each subscription " +
        "opens its own file even when fanned out via SelectMany across parallel branches.")]
    [WorkflowElementCategory(ElementCategory.Transform)]
  
    public class VideoFileWriter : Bonsai.Vision.VideoWriter
    {
        // Inherited properties that are unused at runtime (their values come from the input tuple
        // instead). Hide them from the property grid and the .bonsai XML so the editor doesn't show
        // a confusing always-overridden field and the saved workflow doesn't carry a stale path.

        [Browsable(false)]
        [XmlIgnore]
        public new string FileName
        {
            get { return base.FileName; }
            set { base.FileName = value; }
        }

        [Browsable(false)]
        [XmlIgnore]
        public new double FrameRate
        {
            get { return base.FrameRate; }
            set { base.FrameRate = value; }
        }

        public IObservable<IplImage> Process(IObservable<Tuple<IplImage, string, double>> source)
        {
            return source.Publish(shared =>
                shared.Take(1).SelectMany(value =>
                {
                    var inner = new Bonsai.Vision.VideoWriter
                    {
                        FileName            = value.Item2,
                        FrameRate           = value.Item3,
                        FourCC              = FourCC,
                        FrameSize           = FrameSize,
                        ResizeInterpolation = ResizeInterpolation,
                        Suffix              = Suffix,
                        Buffered            = Buffered,
                        Overwrite           = Overwrite,
                    };

                    var images = shared.Select(pair => pair.Item1).StartWith(value.Item1);
                    return inner.Process(images);
                }));
        }
    }
}
