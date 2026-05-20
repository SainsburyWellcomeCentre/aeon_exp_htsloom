using System;
using System.ComponentModel;
using System.Reactive.Linq;
using Bonsai;
using Bonsai.IO;
using OpenCV.Net;

namespace HtsLoom.Extensions
{
    // OPTION 2 — plain [Combinator] class that composes Bonsai.Vision.VideoWriter instead of inheriting.
    //
    // Pros: only the Tuple-shaped Process is exposed; there is no inherited Process(IObservable<IplImage>)
    //       that could swallow a mis-wired connection. The public surface is one method.
    // Cons: every VideoWriter / FileSink property has to be re-declared here with description and default
    //       attributes mirrored by hand — additions to Bonsai.Vision.VideoWriter in future releases will
    //       not propagate automatically. Migrating an existing cv:VideoWriter node also requires renaming
    //       each <cv:X> property element to <ext:X>; Option 1 keeps those under <cv:> unchanged.
    [Combinator]
    [Description(
        "Writes a sequence of images into a compressed AVI file. The file name and frame rate are " +
        "taken from Item2 and Item3 of the input Tuple<IplImage, string, double>, so each subscription " +
        "opens its own file even when fanned out via SelectMany across parallel branches.")]
    [WorkflowElementCategory(ElementCategory.Transform)]
    public class VideoWriterCombinator
    {
        public VideoWriterCombinator()
        {
            FourCC = "FMP4";
            Buffered = true;
        }

        /// <summary>
        /// Gets or sets a value specifying the four-character code of the codec used to compress video frames.
        /// </summary>
        [Description("Specifies the four-character code of the codec used to compress video frames.")]
        public string FourCC { get; set; }

        /// <summary>
        /// Gets or sets the optional size of video frames.
        /// </summary>
        [Description("The optional size of video frames.")]
        public Size FrameSize { get; set; }

        /// <summary>
        /// Gets or sets a value specifying the optional interpolation method used to resize video frames.
        /// </summary>
        [Description("Specifies the optional interpolation method used to resize video frames.")]
        public SubPixelInterpolation ResizeInterpolation { get; set; }

        /// <summary>
        /// Gets or sets the suffix used to generate file names.
        /// </summary>
        [Description("The suffix used to generate file names.")]
        public PathSuffix Suffix { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether writing should be buffered.
        /// </summary>
        [Description("Indicates whether writing should be buffered.")]
        public bool Buffered { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to overwrite the output file if it already exists.
        /// </summary>
        [Description("Indicates whether to overwrite the output file if it already exists.")]
        public bool Overwrite { get; set; }

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
