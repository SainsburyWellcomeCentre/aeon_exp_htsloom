using System;
using System.ComponentModel;
using System.Reactive.Linq;
using Bonsai;
using Bonsai.IO;
using OpenCV.Net;

namespace HtsLoom.Extensions
{
    // Composes Bonsai.FFmpeg.VideoWriter rather than reimplementing it.
    //
    // The named-pipe plumbing, the ffmpeg process lifetime and the teardown on unsubscribe all stay
    // inside the package node; this class only supplies the per-subscription FileName and FrameRate
    // that a static property cannot express when the sink is fanned out via SelectMany.
    [Combinator]
    [Description(
        "Writes a sequence of images to a video file using an FFmpeg process. The file name and frame " +
        "rate are taken from Item2 and Item3 of the input Tuple<IplImage, string, double>, so each " +
        "subscription opens its own file even when fanned out via SelectMany across parallel branches.")]
    [WorkflowElementCategory(ElementCategory.Transform)]
    public class VideoFFmpegFileWriter
    {
        /// <summary>
        /// Gets or sets the optional suffix used to generate file names.
        /// </summary>
        [Description("The optional suffix used to generate file names.")]
        public PathSuffix Suffix { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the output file should be overwritten if it already exists.
        /// </summary>
        [Description("Indicates whether the output file should be overwritten if it already exists.")]
        public bool Overwrite { get; set; }

        /// <summary>
        /// Gets or sets the optional set of command-line arguments to use for configuring the video codec.
        /// </summary>
        [Description("The optional set of command-line arguments to use for configuring the video codec.")]
        public string OutputArguments { get; set; }

        /// <summary>
        /// Writes an observable sequence of images to a video file using an FFmpeg process.
        /// </summary>
        /// <param name="source">
        /// A sequence of tuples where Item1 is the video frame, Item2 the output file name and Item3
        /// the playback frame rate. The file name and frame rate are read from the first element only.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the image sequence in <paramref name="source"/>
        /// but where there is an additional side effect of writing those images to a video file.
        /// </returns>
        public IObservable<IplImage> Process(IObservable<Tuple<IplImage, string, double>> source)
        {
            return source.Publish(shared =>
                shared.Take(1).SelectMany(value =>
                {
                    // Bonsai.FFmpeg.VideoWriter takes an integer frame rate; the tuple carries the
                    // measured rate as a double, so round rather than truncate and never emit zero.
                    var frameRate = (int)Math.Round(value.Item3);
                    if (frameRate < 1) frameRate = 1;

                    var inner = new Bonsai.FFmpeg.VideoWriter
                    {
                        FileName = value.Item2,
                        FrameRate = frameRate,
                        Suffix = Suffix,
                        Overwrite = Overwrite,
                        OutputArguments = OutputArguments,
                    };

                    // Take(1) has already consumed the first frame from the published sequence, so
                    // push it back in front of the remainder to avoid dropping it from the file.
                    var images = shared.Select(pair => pair.Item1).StartWith(value.Item1);
                    return inner.Process(images);
                }));
        }
    }
}
