﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

[assembly: InternalsVisibleTo("Logic")]
[assembly: InternalsVisibleTo("DataTests")]
namespace Data
{
    internal sealed class DataLogger
    {
        private ConcurrentQueue<CollisionRecord> collisionQueue;
        private string filename;
        private CancellationTokenSource StateChange = new CancellationTokenSource();
        private bool isRunning;
        private readonly object lockObject = new object();
        private const int MaxBufferSize = 1024 * 1024;

        private static DataLogger _instance;

        public static DataLogger GetInstance()
        {
            if (_instance == null)
                throw new InvalidOperationException("Logger nie został zainicjalizowany. Użyj ResetInstance(filename).");
            return _instance;
        }


        public static void ResetInstance(string filename)
        {
            if (_instance != null)
            {
                _instance.stopRunning();
            }
            _instance = new DataLogger(filename);
        }


        private DataLogger(string filename)
        {
            string path = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.Parent.Parent.FullName;
            this.filename = Path.Combine(path, filename);
            File.Create(this.filename).Close();
            collisionQueue = new ConcurrentQueue<CollisionRecord>();
            this.isRunning = true;
            Task.Run(writeDataToLogger);
        }
        public void stopRunning()
        {
            isRunning = false;
        }

        public void LogCollision(CollisionRecord collision)
        {
            lock (lockObject)
            {
                if (collisionQueue.Count < MaxBufferSize)
                {
                    collisionQueue.Enqueue(collision);
                    StateChange.Cancel();
                }
                else
                {
                    Debug.WriteLine("Queue overflow!");
                }
            }
        }


        public async void writeDataToLogger()
        {
            bool isRootWritten = false;
            XmlWriter writer = null;
            try
            {
                using (StreamWriter streamWriter = new StreamWriter(filename, true))
                {
                    writer = XmlWriter.Create(streamWriter, new XmlWriterSettings() { Indent = true, OmitXmlDeclaration = true });
                    writer.WriteStartElement("Balls");
                    writer.Flush();
                    isRootWritten = false;

                    while (this.isRunning)
                    {
                        if (!collisionQueue.IsEmpty)
                        {
                            while (collisionQueue.TryDequeue(out CollisionRecord ball))
                            {
                                DataContractSerializer xmlSer = new DataContractSerializer(typeof(CollisionRecord));
                                xmlSer.WriteObject(writer, ball);
                                writer.Flush();
                                await Task.Delay(Timeout.Infinite, StateChange.Token).ContinueWith(_ => { });
                            }
                        }
                    }
                    isRootWritten = true;
                    writer.WriteEndElement();
                    writer.Flush();
                    writer.Close();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error writing to log: {ex.Message}");
            }
            finally
            {
                if (!isRootWritten && writer != null)
                {
                    writer.WriteEndElement();
                    writer.Flush();
                    writer.Close();
                }
            }
        }
    }
}
