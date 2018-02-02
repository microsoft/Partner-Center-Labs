// -----------------------------------------------------------------------
// <copyright file="ConsoleHelper..cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace HOL.AzureUtilization
{
    using System;
    using System.Collections;
    using System.Globalization;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Store.PartnerCenter.Models;

    /// <summary>
    /// Provides useful helpers that aid in writing to the console. 
    /// </summary>
    /// <remarks>
    /// This helper is based on https://github.com/PartnerCenterSamples/Partner-Center-SDK-Samples/blob/master/Source/Partner%20Center%20SDK%20Samples/Helpers/ConsoleHelper.cs
    /// </remarks>
    public class ConsoleHelper
    {
        /// <summary>
        /// A lazy reference to the singleton console helper instance.
        /// </summary>
        private static Lazy<ConsoleHelper> instance = new Lazy<ConsoleHelper>(() => new ConsoleHelper());

        /// <summary>
        /// A token source which controls cancelling the progress indicator.
        /// </summary>
        private CancellationTokenSource progressCancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// A task that displays progress indicator on the console.
        /// </summary>
        private Task progressBackgroundTask;

        /// <summary>
        /// Gets the single instance of the <see cref="ConsoleHelper"/>.
        /// </summary>
        public static ConsoleHelper Instance
        {
            get
            {
                return instance.Value;
            }
        }

        /// <summary>
        /// Prompts the user to enter a customer identifier.
        /// </summary>
        /// <param name="promptMessage">An optional custom prompt message.</param>
        /// <returns>A string representing the customer identifier.</returns>
        public string ObtainCustomerId(string promptMessage = default(string))
        {
            return ReadNonEmptyString(
                string.IsNullOrWhiteSpace(promptMessage) ? "Enter the customer ID" : promptMessage,
                "The customer identifier cannot be empty");
        }

        /// <summary>
        /// Prompts the user to enter a subscription identifier.
        /// </summary>
        /// <param name="promptMessage">An optional custom prompt message.</param>
        /// <returns>A string representing the customer identifier.</returns>
        public string ObtainSubscriptionId(string promptMessage = default(string))
        {
            return ReadNonEmptyString(
                string.IsNullOrWhiteSpace(promptMessage) ? "Enter the subscription ID" : promptMessage,
                "The customer identifier cannot be empty");
        }

        /// <summary>
        /// Reads a non empty string from the console.
        /// </summary>
        /// <param name="promptMessage">The prompt message to display.</param>
        /// <param name="validationMessage">The error message to show in case of user input error.</param>
        /// <returns>The string input from the console.</returns>
        public string ReadNonEmptyString(string promptMessage, string validationMessage = default(string))
        {
            string input = string.Empty;
            validationMessage = validationMessage ?? "Enter a non-empty value";

            while (true)
            {
                Console.Write($"{promptMessage}: ");
                input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input))
                {
                    Error(validationMessage);
                }
                else
                {
                    break;
                }
            }

            return input.Trim();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="promptMessage">An optional custom prompt message.</param>
        public void Pause(string promptMessage = default(string))
        {
            Console.WriteLine(promptMessage);
            Console.ReadLine();
        }

        /// <summary>
        /// Writes an error message to the console.
        /// </summary>
        /// <param name="message">The message to write.</param>
        /// <param name="newLine">Whether or not to write a new line after the message.</param>
        public void Error(string message, bool newLine = true)
        {
            WriteColored(message, ConsoleColor.Red, newLine);
        }

        /// <summary>
        /// Writes a progress message to the console and starts a progress animation.
        /// </summary>
        /// <param name="message">The progress message to write.</param>
        public void StartProgress(string message)
        {
            if (progressBackgroundTask == null || progressBackgroundTask.Status != TaskStatus.Running)
            {
                progressBackgroundTask = new Task(() =>
                {
                    int dotCounter = 0;

                    while (!progressCancellationTokenSource.Token.IsCancellationRequested)
                    {
                        for (dotCounter = 0; dotCounter < 5; dotCounter++)
                        {
                            WriteColored(".", ConsoleColor.DarkCyan, false);
                            Thread.Sleep(200);

                            if (progressCancellationTokenSource.Token.IsCancellationRequested)
                            {
                                return;
                            }
                        }

                        Console.SetCursorPosition(Console.CursorLeft - dotCounter, Console.CursorTop);

                        for (int i = 0; i < dotCounter; ++i)
                        {
                            Console.Write(" ");
                        }

                        Console.SetCursorPosition(Console.CursorLeft - dotCounter, Console.CursorTop);
                    }
                });

                Console.WriteLine();
                WriteColored(message, ConsoleColor.DarkCyan, false);
                progressBackgroundTask.Start();
            }
        }

        /// <summary>
        /// Stops the progress animation.
        /// </summary>
        public void StopProgress()
        {
            if (progressBackgroundTask != null && progressBackgroundTask.Status == TaskStatus.Running)
            {
                progressCancellationTokenSource.Cancel();
                progressBackgroundTask.Wait();
                progressBackgroundTask.Dispose();
                progressBackgroundTask = null;

                progressCancellationTokenSource.Dispose();
                progressCancellationTokenSource = new CancellationTokenSource();

                Console.WriteLine();
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Writes a message with the requested color to the console.
        /// </summary>
        /// <param name="message">The message to write.</param>
        /// <param name="color">The console color to use.</param>
        /// <param name="newLine">Whether or not to write a new line after the message.</param>
        public void WriteColored(string message, ConsoleColor color, bool newLine = true)
        {
            Console.ForegroundColor = color;
            Console.Write(message + (newLine ? "\n" : string.Empty));
            Console.ResetColor();
        }

        /// <summary>
        /// Writes an object and its properties recursively to the console. Properties are automatically indented.
        /// </summary>
        /// <param name="object">The object to print to the console.</param>
        /// <param name="title">An optional title to display.</param>
        /// <param name="indent">The starting indentation.</param>
        public void WriteObject(object @object, string title = default(string), int indent = 0)
        {
            if (@object == null)
            {
                return;
            }

            const int TabSize = 4;
            bool isTitlePresent = !string.IsNullOrWhiteSpace(title);
            string indentString = new string(' ', indent * TabSize);
            Type objectType = @object.GetType();
            ICollection collection = @object as ICollection;

            if (objectType.Assembly.FullName == typeof(ResourceBase).Assembly.FullName && objectType.IsClass)
            {
                // this is a partner SDK model class, iterate over it's properties recursively
                if (indent == 0 && !string.IsNullOrWhiteSpace(title))
                {
                    Console.WriteLine(title);
                    Console.WriteLine(new string('-', 90));
                }
                else if (isTitlePresent)
                {
                    WriteColored(string.Format(CultureInfo.InvariantCulture, "{0}{1}: ", indentString, title), ConsoleColor.Yellow);
                }
                else
                {
                    // since the current element does not have a title, we do not want to shift it's children causing a double shift appearance
                    // to the user
                    indent--;
                }

                PropertyInfo[] properties = objectType.GetProperties();

                foreach (PropertyInfo property in properties)
                {
                    WriteObject(property.GetValue(@object), property.Name, indent + 1);
                }

                if (indent == 0 && !string.IsNullOrWhiteSpace(title))
                {
                    Console.WriteLine(new string('-', 90));
                }
            }
            else if (collection != null)
            {
                // this is a collection, loop through its element and print them recursively
                WriteColored(string.Format(CultureInfo.InvariantCulture, isTitlePresent ? "{0}{1}: " : string.Empty, indentString, title), ConsoleColor.Yellow);

                foreach (object element in collection)
                {
                    WriteObject(element, indent: indent + 1);
                    Type elementType = element.GetType();

                    if (indent == 1)
                    {
                        Console.WriteLine(new string('-', 80));
                    }
                }
            }
            else
            {
                // print the object as is
                WriteColored(string.Format(CultureInfo.InvariantCulture, isTitlePresent ? "{0}{1}: " : "{0}", indentString, title), ConsoleColor.DarkYellow, false);
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0}", @object));
            }
        }
    }
}
