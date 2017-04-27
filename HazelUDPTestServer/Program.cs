using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

using Hazel;
using Hazel.Udp;

namespace HazelUDPTestServer
{
		public class ServerExample
	{
		static ConnectionListener listener;

		public static void Main(string[] args)
		{
			NetworkEndPoint endPoint = new NetworkEndPoint(IPAddress.Any, 4296);

			listener = new UdpConnectionListener(endPoint);

			listener.NewConnection += NewConnectionHandler;

			Console.WriteLine("Starting server!");

			Console.WriteLine("Server listening on " + (listener as UdpConnectionListener).EndPoint);

			listener.Start();

			Console.WriteLine("Press any key to continue...");

			Console.ReadKey();

			listener.Close();

			Environment.Exit(0);
		}

		static void NewConnectionHandler(object sender, NewConnectionEventArgs args)
		{
			Console.WriteLine("New connection from " + args.Connection.EndPoint.ToString());

			args.Connection.DataReceived += DataReceivedHandler;

			args.Connection.Disconnected += ClientDisconnectHandler;

			args.Recycle();
		}

		private static void DataReceivedHandler(object sender, DataReceivedEventArgs args)
		{
			Connection connection = (Connection)sender;

			Console.WriteLine("Received (" + string.Join<byte>(", ", args.Bytes) + ") from " + connection.EndPoint.ToString());

			connection.SendBytes(args.Bytes, args.SendOption);

			args.Recycle();
		}

		private static void ClientDisconnectHandler(object sender, DisconnectedEventArgs args)
		{
			Connection connection = (Connection)sender;

			Console.WriteLine("Connection from " + connection.EndPoint + " lost");

			args.Recycle();
		}
	}
}