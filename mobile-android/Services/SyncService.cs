using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace AumoFinance.Services;

public enum SyncStatus { Idle, Queueing, Uploading, Success, Failed }

public sealed class SyncStatusEventArgs : EventArgs
{
    public SyncStatus Status { get; }
    public string? Message { get; }
    public SyncStatusEventArgs(SyncStatus status, string? message = null)
    {
        Status = status;
        Message = message;
    }
}

public sealed class SyncService
{
    private readonly ConcurrentQueue<ISyncItem> _queue = new();
    private readonly object _lock = new();
    private bool _isRunning = false;

    // Simple static accessor for views that are created via XAML (no DI support there).
    public static SyncService? Instance { get; private set; }

    public event EventHandler<SyncStatusEventArgs>? StatusChanged;

    public SyncService()
    {
        Instance = this;
    }

    public void Enqueue<T>(T data, Func<T, Task<(bool success, string message)>> uploadTask, Action<T> onDeleteLocalData)
    {
        _queue.Enqueue(new SyncItem<T>(data, uploadTask, onDeleteLocalData));
        OnStatusChanged(SyncStatus.Queueing, "Queued");
        StartWorkerIfNeeded();
    }

    private void StartWorkerIfNeeded()
    {
        lock (_lock)
        {
            if (_isRunning) return;
            _isRunning = true;
            _ = Task.Run(async () => await WorkerLoopAsync());
        }
    }

    private async Task WorkerLoopAsync()
    {
        try
        {
            while (_queue.TryDequeue(out var item))
            {
                OnStatusChanged(SyncStatus.Queueing, "Waiting before upload...");
                // Countdown / debounce before upload (mirror previous behavior)
                await Task.Delay(TimeSpan.FromSeconds(10));

                OnStatusChanged(SyncStatus.Uploading, "Uploading...");

                try
                {
                    var (success, message) = await item.ExecuteAsync();
                    if (success)
                    {
                        item.OnSuccess();
                        OnStatusChanged(SyncStatus.Success, message);
                        await Task.Delay(2000);
                    }
                    else
                    {
                        item.OnFailure();
                        OnStatusChanged(SyncStatus.Failed, message);
                        await Task.Delay(2000);
                    }
                }
                catch (Exception ex)
                {
                    item.OnFailure();
                    OnStatusChanged(SyncStatus.Failed, ex.Message);
                    await Task.Delay(2000);
                }
            }
        }
        finally
        {
            _isRunning = false;
            OnStatusChanged(SyncStatus.Idle, null);
        }
    }

    private void OnStatusChanged(SyncStatus status, string? message)
    {
        StatusChanged?.Invoke(this, new SyncStatusEventArgs(status, message));
    }

    private interface ISyncItem
    {
        Task<(bool success, string message)> ExecuteAsync();
        void OnSuccess();
        void OnFailure();
    }

    private sealed class SyncItem<T> : ISyncItem
    {
        private readonly T _data;
        private readonly Func<T, Task<(bool success, string message)>> _uploadTask;
        private readonly Action<T> _onDeleteLocal;

        public SyncItem(T data, Func<T, Task<(bool success, string message)>> uploadTask, Action<T> onDeleteLocal)
        {
            _data = data;
            _uploadTask = uploadTask;
            _onDeleteLocal = onDeleteLocal;
        }

        public async Task<(bool success, string message)> ExecuteAsync()
        {
            return await _uploadTask(_data);
        }

        public void OnSuccess()
        {
            try { _onDeleteLocal?.Invoke(_data); } catch { }
        }

        public void OnFailure()
        {
            try { _onDeleteLocal?.Invoke(_data); } catch { }
        }
    }
}
