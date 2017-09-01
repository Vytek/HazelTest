using System;
using System.Net;
using System.Collections
using System.Collections.Generic;
using System.Text;
using System.IO;
using FlatBuffers;

using Hazel;
using Hazel.Udp;

using HazelTest;
using HazelMessage;

namespace HazelUDPTestSuperServer
{
    public class Server
	{
        public int portNumber = 4296;

		public bool Running { get; private set; }

		/// <summary>
		/// Send type.
		/// </summary>
        public enum SendType: byte
		{
			SENDTOALL = 0,
			SENDTOOTHER = 1,
			SENDTOSERVER = 2,
            SENDTOUID = 3
		}

        /// <summary>
        /// Command type.
        /// </summary>
		public enum CommandType : sbyte
		{
			LOGIN = 0,
            DISCONNECTEDCLIENT = 1
		}

        //List<Connection> clients = new List<Connection>();
        //https://stackoverflow.com/questions/8629285/how-to-create-a-collection-like-liststring-object
        List<KeyValuePair<String, Connection>> clients = new List<KeyValuePair<String, Connection>>();
        
        /// <summary>
        /// Start this instance.
        /// </summary>
		public void Start()
		{
			NetworkEndPoint endPoint = new NetworkEndPoint(IPAddress.Any, portNumber);
			ConnectionListener listener = new UdpConnectionListener(endPoint);

			Running = true;

			Console.WriteLine("Starting server!");
			Console.WriteLine("Server listening on " + (listener as UdpConnectionListener).EndPoint);
			listener.NewConnection += NewConnectionHandler;
			listener.Start();

			while (Running)
			{
				//Do nothing
			}

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

            //https://stackoverflow.com/questions/943398/get-int-value-from-enum-in-c-sharp
            //https://msdn.microsoft.com/it-it/library/system.enum.getvalues(v=vs.110).aspx
            //http://csharp.net-informations.com/statements/enum.htm
            if (((byte)SendType.SENDTOALL).ToString() == args.Bytes.GetValue(0).ToString())
            {
                //BROADCAST (SENDTOALL)
                Console.WriteLine("BROADCAST (SENDTOALL)");
                //Send data received to all client in List
                foreach (var conn in clients)
				{
				    if (true)
					{
						conn.Value.SendBytes(args.Bytes, args.SendOption);
						Console.WriteLine("Send to: " + conn.Value.EndPoint.ToString());
					}

				}
            } else if ((byte)SendType.SENDTOOTHER == (byte)args.Bytes.GetValue(0))
            {
                //BROADCAST (SENDTOOTHER)
                Console.WriteLine("BROADCAST (SENDTOOTHER)");
                //Send data received to all other client in List
                foreach (var conn in clients)
				{
					if (conn.Value != connection) //SENDTOOTHER
					{
                        conn.Value.SendBytes(args.Bytes, args.SendOption);
						Console.WriteLine("Send to: " + conn.Value.EndPoint.ToString());
					}

				} 
            } else if ((byte)SendType.SENDTOSERVER == (byte)args.Bytes.GetValue(0))
            {
                //FOR NOW ECHO SERVER (SENDTOSERVER)
                Console.WriteLine("FOR NOW ECHO SERVER (SENDTOSERVER)");
                connection.SendBytes(args.Bytes, args.SendOption);
				//Parser Message
				//Remove first byte (type)
				//https://stackoverflow.com/questions/31550484/faster-code-to-remove-first-elements-from-byte-array
				byte STypeBuffer = args.Bytes[0]; 
				byte[] NewBufferReceiver = new byte[args.Bytes.Length - 1];
				Array.Copy(args.Bytes, 1, NewBufferReceiver, 0, NewBufferReceiver.Length);
				ByteBuffer bb = new ByteBuffer(NewBufferReceiver);
                //Decoder FlatBuffer
                if (STypeBuffer == 4)
                {
                    HazelMessage.HMessage HMessageReceived = HazelMessage.HMessage.GetRootAsHMessage(bb);
                    if ((byte)CommandType.LOGIN == HMessageReceived.Command)
                    {
						//Cerca e restituisci il tutto
						foreach (var conn in clients)
						{
							if (conn.Value == connection) //SENDTOSERVER
							{
                                String UIDBuffer = conn.Key;
                                Console.WriteLine("UID: " + UIDBuffer);
							}

						}
					}
                }
                //Encode FlatBuffer
                //Reply to Client
                Console.WriteLine("Send to: " + connection.EndPoint.ToString());
			}
			args.Recycle();
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
            //https://stackoverflow.com/posts/1608949/revisions
            clients.RemoveAll(item => item.Value.Equals(connection));
			args.Recycle();
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
				Server ServerHazel = new Server();
				ServerHazel.Start();
			}
		}
	}
}
