using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;

namespace ConsoleApp5
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var program = new Program();
            program.OnStart(args);

            // If User enters 'Stop' - Start shut down sequence of Console App.
            var stopCommand = Console.ReadLine().ToUpper();
            if (stopCommand == "STOP")
                program.OnStop();
        }

        private static Thread _WorkerThread { get; set; }
        private ManualResetEvent _ShutdownEvent = new ManualResetEvent(false);

        private void OnStart(string[] args)
        {
            Console.WriteLine("Automation test execution service starting...");

            // TODO: Initialize DB and pull in queued tests
            // TODO: Create Loop Starter to check for new data every 1 minute (Async Task Factory)
            _WorkerThread = new Thread(WorkerThreadFunc)
            {
                Name = "Start Loop Thread",
                IsBackground = true
            };

            Console.WriteLine("Automation test execution service started.");
            _WorkerThread.Start();
        }

        private void OnStop()
        {
            Console.WriteLine("Automation test execution service stopping...");

            // Cleanup Threads and finish writing any tests.
            _ShutdownEvent.Set();
            Console.WriteLine("Shutting down queue.");
            if (!_WorkerThread.Join(3000))
            {
                _WorkerThread.Abort();
            }

            Console.WriteLine("Automation test execution service stopped.");
        }

        private void WorkerThreadFunc()
        {
            //TODO: Make sure only todays dates run in the queue. Example if something is queued for tomorrow skip.
            while (true)
            //while (!_ShutdownEvent.WaitOne(0))
            {
                try
                {
                    Console.WriteLine("Querying for new tests in Queue...");
                    QueuedTestsModel queuedTest = new QueuedTestsModel();

                    var queuedList = queuedTest.QueuedTests.ToList();
                    Console.WriteLine($"Found {queuedList.Count} tests in current queue.");

                    var count = 0;

                    if (queuedList.Count > 0)
                    {
                        // Get first item in queue
                        var item = queuedList[count];
                        bool noReadyTests = true;
                        foreach (var queuedTestItem in queuedList)
                        {
                            if (queuedTestItem.QueuedDateTime <= DateTime.Now)
                            {
                                item = queuedTestItem;
                                noReadyTests = false;
                                goto TestReady;
                            }
                        }

                        if (noReadyTests)
                        {
                            goto NoTestsReady;
                        }

                        TestReady:

                        Console.WriteLine(
                            $"Request sent to start test with following parameters:\n" +
                            $"\t - Test Name: {item.TestName}, \n" +
                            $"\t - Application Name: {item.ApplicationName}, \n" +
                            $"\t - User name: {item.UserName}, \n" +
                            $"\t - Queued Date: {item.QueuedDateTime}\n" +
                            $"\t - Stack Trace: {item.StackTrace}, \n" +
                            $"\t - Utilization: {item.Utilization}, \n" +
                            $"\t - Console Log: {item.ConsoleLog}, \n" +
                            $"\t - Environment: {item.Environment}, \n");

                        // TODO: Start Test
                        // Run test logic
                        var argument = string.Empty;
                        var output = string.Empty;

                        switch (item.ApplicationName)
                        {
                            case "FMWEB":
                                {
                                    argument = @"C:\Users\sgrach\Desktop\Dlls\FMWEB\FMGUI\FMGui.dll";
                                    argument += " \"C:\\Users\\sgrach\\Desktop\\Dlls\\FMWEB\\RESTLESSAPI\\RestlessApi.dll\"";
                                    argument += $" /Tests:{item.TestName}";
                                    output = StartProcess(argument);
                                    break;
                                }
                            case "MMAv3":
                                {
                                    argument = @"C:\Users\sgrach\Desktop\Dlls\MMAv3\MMAGUI\FMGui.dll";
                                    argument += $" /Tests:{item.TestName}";
                                    output = StartProcess(argument);
                                    break;
                                }
                            case "CIC":
                                {
                                    argument = @"C:\Users\sgrach\Desktop\Dlls\CIC\WebWorkSpace.dll";
                                    argument += $" /Tests:{item.TestName}";
                                    output = StartProcess(argument);
                                    break;
                                }
                            default:
                                {
                                    // TODO: If app doesn't exist, consider deleting app from queue so that it doesn't keep trying to pull the same record (Add to log, saying test not started because of no app support).
                                    argument = @"\help";
                                    output = StartProcess(argument);
                                    break;
                                }
                        }

                        var saveSuccesful = 0;
                        try
                        {
                            // Delete test from Queued Test Model and Add Queued Test Model to History
                            QueuedTests_HistoryModel historyModel = new QueuedTests_HistoryModel();
                            historyModel.QueuedTests_History.Add(new QueuedTests_History
                            {
                                TestName = item.TestName,
                                UserName = item.UserName,
                                ApplicationName = item.ApplicationName,
                                QueuedDateTime = item.QueuedDateTime,
                                CompletedDateTime = DateTime.Now,
                                Log = output,
                                Environment = item.Environment,
                                StackTrace = item.StackTrace,
                                Utilization = item.Utilization,
                                ConsoleLog = item.ConsoleLog
                            });

                            saveSuccesful = historyModel.SaveChanges();
                            Console.WriteLine($"Added item id - {item.Id} to QueuedTests_History table.");
                        }
                        catch (SqlException ex)
                        {
                            Console.WriteLine("Exception occured: " + ex.Message + "\n" + ex.StackTrace + "\n");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Exception occured: {ex.Message} \n{ex.StackTrace}");
                        }

                        if (saveSuccesful == 1)
                        {
                            try
                            {
                                queuedTest.QueuedTests.Remove(item);
                                queuedTest.SaveChanges();
                                Console.WriteLine($"Removed item - {item.Id} from queue.");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Exception occured: {ex.Message} \n{ex.StackTrace}");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Save to QueuedTests_History table not succesful.");
                        }

                        NoTestsReady:
                        if (noReadyTests)
                        {
                            Console.WriteLine("No current tests found in queue.. Waiting 60 seconds.");
                            Thread.Sleep(60000);
                        }
                    }
                    else
                    {
                        Console.WriteLine("No current tests found in queue.. Waiting 60 seconds.");
                        Thread.Sleep(60000);
                    }
                }
                catch (SqlException ex)
                {
                    Console.WriteLine(ex.Message + "\n" + ex.StackTrace + "\n");
                    Console.WriteLine("Something went wrong... Waiting 5 minutes and trying again.");
                    Thread.Sleep(300000);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message + "\n" + ex.StackTrace + "\n");
                    Console.WriteLine("Something went wrong... Waiting 5 minutes and trying again.");
                    Thread.Sleep(300000);
                }
            }
        }


        private string StartProcess(string argument)
        {
            string output = string.Empty;
            const string fileName =
                @"C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe";
            // Local: @"C:\Program Files(x86)\Microsoft Visual Studio\2017\Enterprise\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe";

            try
            {
                Console.WriteLine("Starting Process options.");
                var process = new Process
                {
                    StartInfo =
                    {
                        UseShellExecute = false,
                        CreateNoWindow = false,
                        ErrorDialog = false,
                        FileName = fileName,
                        Arguments = argument,
                        RedirectStandardOutput = true
                    }
                };

                Console.WriteLine("Starting Process...");

                process.Start();
                output = process.StandardOutput.ReadToEnd();
                Console.WriteLine($"Process output: {output} ");

                // TODO: Consider grabbing the output and only if test ran succesfully should the test be added to history queue, otherwise try again.

                process.WaitForExit();

                Console.WriteLine("Process exiting...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Process exception error: {ex.Message}");
                output = ex.Message;
            }

            return output;
        }
    }
}
