﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Hazel;
using Hazel.Tcp;

namespace HazelTestSuperClient
{
	class ClientExample
	{
		static Connection connection;

		public static void Main(string[] args)
		{
			NetworkEndPoint endPoint = new NetworkEndPoint("127.0.0.1", 4296);

			connection = new TcpConnection(endPoint);

			connection.DataReceived += DataReceived;
			connection.Disconnected += ServerDisconnectHandler;

			ConsoleKeyInfo cki;

			try
			{
				Console.WriteLine("Connecting!");

				connection.Connect();

				connection.SendBytes(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });

				do 
		      	{
					cki = Console.ReadKey();
					if (cki.Key == ConsoleKey.S)
					{
						Console.WriteLine();
						connection.SendBytes(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });
						//http://johnatten.com/2014/09/07/c-building-a-useful-extensible-net-console-application-template-for-development-and-testing/
					}
		       	} while (cki.Key != ConsoleKey.Escape);

				connection.Close();

				Environment.Exit(0);
			}
			catch (Hazel.HazelException ex)
			{
				Console.Error.WriteLine("Error: " + ex.Message + " from " + ex.Source);
				connection.Close();
				Environment.Exit(1);
			}
		}

		private static void DataReceived(object sender, DataReceivedEventArgs args)
		{
			Console.WriteLine("Received (" + string.Join<byte>(", ", args.Bytes) + ") from " + connection.EndPoint.ToString());

			args.Recycle();
		}

		private static void ServerDisconnectHandler(object sender, DisconnectedEventArgs args)
		{
			Connection connection = (Connection)sender;

			Console.WriteLine("Server connection at " + connection.EndPoint + " lost");

			connection = null;

			args.Recycle();
		}	
	}
}
