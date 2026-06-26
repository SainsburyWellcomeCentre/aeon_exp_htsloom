using Bonsai;
using System;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;

[Combinator]
[Description("Maintains a bounded sliding-window replay buffer of the last N elements for each group key. " +
             "New subscribers to a group immediately receive the retained history (up to BufferSize elements) " +
             "followed by any subsequent live elements. Unlike Buffer/Window, a partially filled buffer is " +
             "replayed immediately, which makes it suitable for sparse event streams (e.g. beam breaks per feeder).")]
[WorkflowElementCategory(ElementCategory.Combinator)]
public class ReplayGroupBuffer
{
    int bufferSize = 300;

    [Description("The maximum number of recent elements to retain and replay for each group.")]
    public int BufferSize
    {
        get { return bufferSize; }
        set { bufferSize = value; }
    }

    public IObservable<IGroupedObservable<TKey, TSource>> Process<TKey, TSource>(
        IObservable<IGroupedObservable<TKey, TSource>> source)
    {
        var capacity = bufferSize;
        return Observable.Create<IGroupedObservable<TKey, TSource>>(observer =>
        {
            // Every group's source subscription is kept alive for the lifetime of the
            // outer subscription so all buffers fill continuously, regardless of which
            // group is currently observed downstream.
            var subscriptions = new CompositeDisposable();
            var groups = source.Subscribe(
                group =>
                {
                    var replay = new ReplaySubject<TSource>(capacity);
                    subscriptions.Add(group.Subscribe(replay));
                    observer.OnNext(new ReplayGroup<TKey, TSource>(group.Key, replay));
                },
                observer.OnError,
                observer.OnCompleted);
            subscriptions.Add(groups);
            return subscriptions;
        });
    }

    class ReplayGroup<TKey, TSource> : IGroupedObservable<TKey, TSource>
    {
        readonly TKey key;
        readonly IObservable<TSource> source;

        public ReplayGroup(TKey key, IObservable<TSource> source)
        {
            this.key = key;
            this.source = source;
        }

        public TKey Key
        {
            get { return key; }
        }

        public IDisposable Subscribe(IObserver<TSource> observer)
        {
            return source.Subscribe(observer);
        }
    }
}
