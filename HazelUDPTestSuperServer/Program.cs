using System;
using System.Net;
using System.Collections;
using System.Collections.Generic;

using Hazel;
using Hazel.Udp;

namespace HazelUDPTestSuperServer
{
	public class Server
	{
		public static Hashtable clientList = new Hashtable();

		public int portNumber = 4296;

		private int counter = 0;

		public bool Running { get; private set; }

		/// <summary>
		/// Send type.
		/// </summary>
        public enum SendType: byte
		{
			SENDTOALL = 0,
			SENDTOOTHER = 1,
			SENDTOSERVER = 2
		}

		List<Connection> clients = new List<Connection>();

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
			Console.WriteLine("New connection from " + args.Connection.EndPoint.ToString());
			clients.Add(args.Connection);
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

            if (args.Bytes.GetValue(0).Equals(SendType.SENDTOALL))
            {
				//BROADCAST ((SENDTOALL)
				//Send data received to all client in List
				foreach (var conn in clients)
				{
				    if (true)
					{
						conn.SendBytes(args.Bytes, args.SendOption);
						Console.WriteLine("Send to: " + conn.EndPoint.ToString());
					}

				}
            } else if (args.Bytes.GetValue(0).Equals(SendType.SENDTOOTHER))
            {
				//BROADCAST (SENDTOOTHER)
				//Send data received to all other client in List
				foreach (var conn in clients)
				{
					if (conn != connection) //SENDTOOTHER
					{
						conn.SendBytes(args.Bytes, args.SendOption);
						Console.WriteLine("Send to: " + conn.EndPoint.ToString());
					}

				} 
            } else if (args.Bytes.GetValue(0).Equals(SendType.SENDTOSERVER))
            {
				//FOR NOW ECHO SERVER (SENDTOSERVER)
				connection.SendBytes(args.Bytes, args.SendOption);
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
			clients.Remove(connection);
			args.Recycle();
		}

		public void Shutdown()
		{
			if (Running)
			{
				Running = false;
				Console.WriteLine("Shutting down the Hazel Server...");
			}
		}

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
