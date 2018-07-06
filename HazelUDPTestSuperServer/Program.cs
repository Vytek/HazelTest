using System;
using System.Net;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;
using System.IO;
using FlatBuffers;

using Hazel;
using Hazel.Udp;

using HazelTest;
using HazelMessage;
using System.Threading;
using LiteDB;
using Scrypt;
using System.Reflection;

namespace HazelUDPTestSuperServer
{
    public class Server
	{
        public int portNumber = 4296;

		public bool Running { get; private set; }

        /// <summary>
        /// Users class.
        /// </summary>
        public class Users
        {
            public int Id { get; set; }
            public string UserName { get; set; }
            public string UserPassword { get; set; }
            public bool IsActive { get; set; }
        }

        /// <summary>
        /// Send type.
        /// </summary>
        public enum SendType: byte
		{
			SENDTOALL = 0,
			SENDTOOTHER = 1,
			SENDTOSERVER = 2,
            SENDTOUID = 3 //NOT IMPLEMENTED
		}

        /// <summary>
        /// Packet identifier.
        /// </summary>
        public enum PacketId : sbyte
        {
            PLAYER_JOIN = 0,
            OBJECT_MOVE = 1,
            PLAYER_SPAWN = 2,
            OBJECT_SPAWN = 3,
            PLAYER_MOVE = 4,
            MESSAGE_SERVER = 5,
            OBJECT_UNSPAWN = 6
        }

        /// <summary>
        /// Command type.
        /// </summary>
		public enum CommandType : sbyte
		{
			LOGIN = 0,
            DISCONNECTEDCLIENT = 1
		}

        /// <summary>
        /// Client message received.
        /// </summary>
		public struct ClientMessageReceived
		{
            public byte[] MessageBytes;
            public Connection ClientConnected;
            public Hazel.SendOption SOClientConnected;
		};

        //List<Connection> clients = new List<Connection>();
		//https://stackoverflow.com/questions/8629285/how-to-create-a-collection-like-liststring-object
		static List<KeyValuePair<String, Connection>> clients = new List<KeyValuePair<String, Connection>>();
        //Queue Messages
        static ConcurrentQueue<ClientMessageReceived> QueueMessages = new ConcurrentQueue<ClientMessageReceived>();

        static ManualResetEvent _quitEvent = new ManualResetEvent(false);
        
        //LiteDB connection
        static LiteDatabase db = new LiteDatabase(Path.Combine(AssemblyDirectory, @"Users.db"));

        /// <summary>
        /// Start this instance.
        /// </summary>
		public void Start()
		{
            //https://stackoverflow.com/questions/2586612/how-to-keep-a-net-console-app-running
            Console.CancelKeyPress += (sender, eArgs) => {
				_quitEvent.Set();
				eArgs.Cancel = true;
			};

            //Connect and create users collection for LiteDB.org
            //Get users collection
            var col = db.GetCollection<Users>("users");

            if (col.Count() == 0)
            {
                ScryptEncoder encoder = new ScryptEncoder();
                string hashsedPassword = encoder.Encode("test1234!");
                //Console.WriteLine(hashsedPassword);
                //Same password
                //string hashsedPassword2 = encoder.Encode("test1234!");
                //Console.WriteLine(hashsedPassword);
                // Create your new customer instance
                var user = new Users
                {
                    UserName = "Vytek75",
                    UserPassword = hashsedPassword,
                    IsActive = true
                };

                // Create unique index in Name field
                col.EnsureIndex(x => x.UserName, true);

                // Insert new customer document (Id will be auto-incremented)
                col.Insert(user);
            }
           
            NetworkEndPoint endPoint = new NetworkEndPoint(IPAddress.Any, portNumber);
			ConnectionListener listener = new UdpConnectionListener(endPoint);

			Running = true;

			Console.WriteLine("Starting server!");
            Console.WriteLine("BD file path: " + Path.Combine(AssemblyDirectory, @"Users.db"));
			Console.WriteLine("Server listening on " + (listener as UdpConnectionListener).EndPoint);
			listener.NewConnection += NewConnectionHandler;
			listener.Start();

			_quitEvent.WaitOne();

			//Close all
			listener.Close();
			//Exit 0
			Environment.Exit(0);
		}

        /// <summary>
        /// News the connection handler.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="args">Arguments.</param>
		private void NewConnectionHandler(object sender, NewConnectionEventArgs args)
		{
            string UID = RandomIdGenerator.GetBase62(6);
            Console.WriteLine("UID Created: " + UID);
            //https://www.dotnetperls.com/keyvaluepair
            clients.Add(new KeyValuePair<string, Connection>(UID, args.Connection));
            Console.WriteLine("New connection from " + args.Connection.EndPoint.ToString() + " with UID: "+ UID);
            args.Connection.DataReceived += this.DataReceivedHandler;
			args.Connection.Disconnected += this.ClientDisconnectHandler;
			args.Recycle();
		}

        /// <summary>
        /// Datas the received handler.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="args">Arguments.</param>
		private void DataReceivedHandler(object sender, DataReceivedEventArgs args)
		{
			Connection connection = (Connection)sender;
			Console.WriteLine("Received (" + string.Join<byte>(", ", args.Bytes) + ") from " + connection.EndPoint.ToString());
            Console.WriteLine("SendType: " + args.Bytes.GetValue(0).ToString());
			//Console.WriteLine(((byte)SendType.SENDTOALL).ToString());

			//Create Struct ClientMessageReceived
			ClientMessageReceived NewClientConnected;
            NewClientConnected.ClientConnected = connection;
            NewClientConnected.MessageBytes = args.Bytes;
            NewClientConnected.SOClientConnected = args.SendOption;

            //Add To main Queue
            QueueMessages.Enqueue(NewClientConnected);
            ThreadPool.QueueUserWorkItem(Server.ConsumerThread);
            args.Recycle();
		}

        /// <summary>
        /// Consumers the thread.
        /// </summary>
        /// <param name="arg">Argument.</param>
        private static void ConsumerThread(object arg)
        {
            ClientMessageReceived item;
            //while (true)
            Console.WriteLine("Queue: " + Server.QueueMessages.Count.ToString());
            while (!Server.QueueMessages.IsEmpty)
            {
                bool isSuccessful = Server.QueueMessages.TryDequeue(out item);
                Console.WriteLine("Dequeue: " + isSuccessful);
                if (isSuccessful)
                {
                    //https://stackoverflow.com/questions/943398/get-int-value-from-enum-in-c-sharp
                    //https://msdn.microsoft.com/it-it/library/system.enum.getvalues(v=vs.110).aspx
                    //http://csharp.net-informations.com/statements/enum.htm
                    if (((byte)SendType.SENDTOALL).ToString() == item.MessageBytes.GetValue(0).ToString())
                    {
                        //BROADCAST (SENDTOALL)
                        Console.WriteLine("BROADCAST (SENDTOALL)");
                        //Send data received to all client in List
                        foreach (var conn in Server.clients)
                        {
                            if (true)
                            {
                                conn.Value.SendBytes(item.MessageBytes, item.SOClientConnected);
                                Console.WriteLine("Send to: " + conn.Value.EndPoint.ToString());
                            }

                        }
                    }
                    else if ((byte)SendType.SENDTOOTHER == (byte)item.MessageBytes.GetValue(0))
                    {
                        //BROADCAST (SENDTOOTHER)
                        Console.WriteLine("BROADCAST (SENDTOOTHER)");
                        //Send data received to all other client in List
                        foreach (var conn in Server.clients)
                        {
                            if (conn.Value != item.ClientConnected) //SENDTOOTHER
                            {
                                conn.Value.SendBytes(item.MessageBytes, item.SOClientConnected);
                                Console.WriteLine("Send to: " + conn.Value.EndPoint.ToString());
                            }

                        }
                    }
                    else if ((byte)SendType.SENDTOSERVER == (byte)item.MessageBytes.GetValue(0))
                    {
                        //FOR NOW ECHO SERVER (SENDTOSERVER)
                        Console.WriteLine("CLIENT TO SERVER (SENDTOSERVER)");
                        //Parser Message
                        //Remove first byte (type)
                        //https://stackoverflow.com/questions/31550484/faster-code-to-remove-first-elements-from-byte-array
                        byte STypeBuffer = item.MessageBytes[0];
                        byte[] NewBufferReceiver = new byte[item.MessageBytes.Length - 1];
                        Array.Copy(item.MessageBytes, 1, NewBufferReceiver, 0, NewBufferReceiver.Length);
                        ByteBuffer bb = new ByteBuffer(NewBufferReceiver);
                        //Decoder FlatBuffer
                        String UIDBuffer = String.Empty;
                        if (STypeBuffer == 2)
                        {
                            HazelMessage.HMessage HMessageReceived = HazelMessage.HMessage.GetRootAsHMessage(bb);
                            if ((sbyte)CommandType.LOGIN == HMessageReceived.Command)
                            {
                                //Cerca e restituisci il tutto
                                foreach (var conn in Server.clients)
                                {
                                    if (conn.Value == item.ClientConnected) //SENDTOSERVER
                                    {
                                        //TODO: Check here if user exist and password correct
                                        //Get users collection
                                        var col = db.GetCollection<Users>("users");
                                        Console.WriteLine("COMMAND RECIEVED: " + HMessageReceived.Answer);
                                        //Parse HMessageReceived
                                        string[] words = HMessageReceived.Answer.Split(';');
                                        //words[0] = Login; words[1] = Password
                                        if (col.Count(Query.EQ("UserName", words[0]))==1)
                                        {
                                            var results = col.Find(Query.EQ("UserName", words[0]));
                                            string UserPasswordRecord = string.Empty;
                                            foreach (var c in results)
                                            {
                                                Console.WriteLine("#{0} - {1}", c.Id, c.UserName);
                                                UserPasswordRecord = c.UserPassword;
                                            }
                                            //Verify password
                                            ScryptEncoder encoder = new ScryptEncoder();
                                            //Check password
                                            if (encoder.Compare(words[1], UserPasswordRecord))
                                            {
                                                //OK
                                                UIDBuffer = conn.Key;
                                                Console.WriteLine("UID: " + UIDBuffer);
                                            }
                                            else
                                            {
                                                //*NOT* OK
                                                UIDBuffer = string.Empty;
                                                Console.WriteLine("UID: ERROR PASSWORD" + UIDBuffer);
                                            }
                                        }
                                        else
                                        {
                                            UIDBuffer = string.Empty;
                                            Console.WriteLine("UID: USER NOT EXISTS!" + UIDBuffer);
                                        }
                                    }
                                }
                            }
                        }
                        //Encode FlatBuffer
                        //Create flatbuffer class
                        FlatBufferBuilder fbb = new FlatBufferBuilder(1);

                        StringOffset SOUIDBuffer = fbb.CreateString(UIDBuffer);

                        HazelMessage.HMessage.StartHMessage(fbb);
                        HazelMessage.HMessage.AddCommand(fbb, (sbyte)CommandType.LOGIN);
                        HazelMessage.HMessage.AddAnswer(fbb, SOUIDBuffer);
                        var offset = HazelMessage.HMessage.EndHMessage(fbb);
                        HazelMessage.HMessage.FinishHMessageBuffer(fbb, offset);
                        //Reply to Client
                        using (var ms = new MemoryStream(fbb.DataBuffer.Data, fbb.DataBuffer.Position, fbb.Offset))
                        {
                            //Add type!
                            //https://stackoverflow.com/questions/5591329/c-sharp-how-to-add-byte-to-byte-array
                            byte[] newArray = new byte[ms.ToArray().Length + 1];
                            ms.ToArray().CopyTo(newArray, 1);
                            newArray[0] = (byte)SendType.SENDTOSERVER;
                            item.ClientConnected.SendBytes(newArray, item.SOClientConnected);
                        }
                        Console.WriteLine("Send to: " + item.ClientConnected.EndPoint.ToString());
                        //HERE SEND TO ALL CLIENTS OBJECTS DB
                    }
                }
            }
        }

        /// <summary>
        /// Clients the disconnect handler.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="args">Arguments.</param>
		private void ClientDisconnectHandler(object sender, DisconnectedEventArgs args)
		{
			Connection connection = (Connection)sender;
			Console.WriteLine("Connection from " + connection.EndPoint + " lost");
            String UIDBuffer = String.Empty;
			//Cerca e restituisci il tutto
			foreach (var conn in clients)
			{
				if (conn.Value == connection) //SENDTOSERVER
				{
                    UIDBuffer = conn.Key;
					Console.WriteLine("UID TO DESTROY: " + UIDBuffer);
				}

			}

			//https://stackoverflow.com/posts/1608949/revisions //Debug
            //Delete client disconnected
			clients.RemoveAll(item => item.Value.Equals(connection));

			//Encode FlatBuffer
			//Create flatbuffer class
			FlatBufferBuilder fbb = new FlatBufferBuilder(1);

			StringOffset SOUIDBuffer = fbb.CreateString(UIDBuffer);

			HazelMessage.HMessage.StartHMessage(fbb);
			HazelMessage.HMessage.AddCommand(fbb, (sbyte)CommandType.DISCONNECTEDCLIENT);
			HazelMessage.HMessage.AddAnswer(fbb, SOUIDBuffer);
			var offset = HazelMessage.HMessage.EndHMessage(fbb);
			HazelMessage.HMessage.FinishHMessageBuffer(fbb, offset);
			//Reply to all Client
			using (var ms = new MemoryStream(fbb.DataBuffer.Data, fbb.DataBuffer.Position, fbb.Offset))
			{
				//Add type!
				//https://stackoverflow.com/questions/5591329/c-sharp-how-to-add-byte-to-byte-array
				byte[] newArray = new byte[ms.ToArray().Length + 1];
				ms.ToArray().CopyTo(newArray, 1);
				newArray[0] = (byte)SendType.SENDTOSERVER;
				foreach (var conn in clients)
				{
                    conn.Value.SendBytes(newArray, SendOption.Reliable);
					Console.WriteLine("Send to: " + conn.Value.EndPoint.ToString());
				}
			}
			args.Recycle();
        }
        
        /// <summary>
        /// Return path of main assembly
        /// </summary>
        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        /// <summary>
        /// Shutdown this instance.
        /// </summary>
		public void Shutdown()
		{
			if (Running)
			{
				Running = false;
				Console.WriteLine("Shutting down the Hazel Server...");
			}
		}

        //https://stackoverflow.com/posts/9543797/revisions
        //https://stackoverflow.com/questions/9543715/generating-human-readable-usable-short-but-unique-ids?answertab=votes#tab-top
        /// <summary>
        /// Random identifier generator.
        /// </summary>
        public static class RandomIdGenerator
        {
            private static char[] _base62chars =
                "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz"
                .ToCharArray();

            private static Random _random = new Random();

            public static string GetBase62(int length)
            {
                var sb = new StringBuilder(length);

                for (int i = 0; i < length; i++)
                    sb.Append(_base62chars[_random.Next(62)]);

                return sb.ToString();
            }

            public static string GetBase36(int length)
            {
                var sb = new StringBuilder(length);

                for (int i = 0; i < length; i++)
                    sb.Append(_base62chars[_random.Next(36)]);

                return sb.ToString();
            }
        }

        /// <summary>
        /// Main class.
        /// </summary>
        class MainClass
		{
			public static void Main(string[] args)
			{
                ThreadPool.QueueUserWorkItem(Server.ConsumerThread);
                Server ServerHazel = new Server();
				ServerHazel.Start();
			}
		}
	}
}
