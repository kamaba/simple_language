using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SimpleLanguage.Logging;

namespace SimpleLanguage.Compile.Process
{
    /// <summary>
    /// Central controller that runs compilation phases, supports cancellation/abort and basic monitoring.
    /// Designed to coordinate mixed sequential/parallel phases (e.g. multi-threaded FileMeta, single-threaded Core/Meta, multi-threaded IR).
    /// </summary>
    public class ProcessController
    {
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly object _lock = new object();

        public bool IsAborted => _cts.IsCancellationRequested;

        public event Action<ErrorDefinition, string> Aborted; // notify subscribers about abort reason

        public ProcessController()
        {
        }

        public void Abort(int errorId, string message)
        {
            // mark cancellation and notify
            lock (_lock)
            {
                if (!_cts.IsCancellationRequested)
                {
                    _cts.Cancel();
                    // log and notify
                    try
                    {
                        if (ErrorRegistry.Instance.TryGet(errorId, out var def))
                        {
                            var logger = LogManager.GetLogger(def.Module);
                            logger.Log(errorId, message);
                            Aborted?.Invoke(def, message);
                        }
                        else
                        {
                            // fallback log
                            var logger = LogManager.GetLogger(ErrorModule.FileMeta);
                            logger.Log(99999, message);
                            Aborted?.Invoke(new ErrorDefinition { Id = errorId, MessageTemplate = message }, message);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Abort logging failed: " + ex.Message);
                    }
                }
            }
        }

        public CancellationToken Token => _cts.Token;

        /// <summary>
        /// Run a sequence of phases. Each phase may run parallel work and should observe CancellationToken.
        /// </summary>
        public async Task RunPhasesAsync(IEnumerable<Func<CancellationToken, Task>> phases)
        {
            foreach (var phase in phases)
            {
                if (IsAborted) break;
                try
                {
                    await phase(Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // canceled - stop pipeline
                    break;
                }
                catch (CompilationAbortException cex)
                {
                    // stop and forward abort
                    Abort(cex.ErrorId, cex.Message);
                    break;
                }
                catch (Exception ex)
                {
                    // unexpected error - log and abort
                    Abort(99999, "Unhandled exception in phase: " + ex.Message);
                    break;
                }
            }
        }

        /// <summary>
        /// Helper to run multiple worker actions in parallel for a phase.
        /// Each worker should accept CancellationToken and return Task.
        /// The degree of parallelism can be controlled by maxConcurrency; 0 means unbounded.
        /// </summary>
        public async Task RunParallelWorkersAsync(IEnumerable<Func<CancellationToken, Task>> workers, int maxConcurrency = 0)
        {
            if (IsAborted) return;

            var options = new ParallelOptions { CancellationToken = Token, MaxDegreeOfParallelism = maxConcurrency > 0 ? maxConcurrency : Environment.ProcessorCount };
            var tasks = new List<Task>();
            using (var sem = new SemaphoreSlim(options.MaxDegreeOfParallelism))
            {
                foreach (var worker in workers)
                {
                    await sem.WaitAsync(Token).ConfigureAwait(false);
                    if (IsAborted)
                    {
                        sem.Release();
                        break;
                    }

                    var t = Task.Run(async () =>
                    {
                        try
                        {
                            await worker(Token).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException) { }
                        catch (CompilationAbortException cex)
                        {
                            Abort(cex.ErrorId, cex.Message);
                        }
                        catch (Exception ex)
                        {
                            Abort(99999, "Unhandled worker exception: " + ex.Message);
                        }
                        finally
                        {
                            sem.Release();
                        }
                    }, Token);

                    tasks.Add(t);
                }

                try
                {
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { }
            }
        }
    }
}
