﻿using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.UI.Chat;
using TerrariaChatRelay.Clients;
using TerrariaChatRelay.Command;
using TerrariaChatRelay.Command.Commands;
using TerrariaChatRelay.Helpers;

namespace TerrariaChatRelay
{
    public class Core
    {
        /// <summary>
        /// IChatClients list for clients to register with.
        /// </summary>
        public static List<IChatClient> Subscribers { get; set; }
		public static ICommandService CommandServ { get; set; }

        public static event EventHandler<TerrariaChatEventArgs> OnGameMessageReceived;
        public static event EventHandler<TerrariaChatEventArgs> OnGameMessageSent;

		public static event EventHandler<ClientChatEventArgs> OnClientMessageReceived;
		public static event EventHandler<ClientChatEventArgs> OnClientMessageSent;

		/// <summary>
		/// Intializes all values to default values to ready EventManager for use.
		/// </summary>
		public static void Initialize()
		{
			Subscribers = new List<IChatClient>();
			CommandServ = new CommandService();
		}


		/// <summary>
		/// Emits a message to TerrariaChatRelay that a client message have been received.
		/// </summary>
		/// <param name="sender">Object that is emitting this event.</param>
		/// <param name="user">User object detailing the client source and username</param>
		/// <param name="msg">Text content of the message</param>
		/// <param name="commandPrefix">Command prefix to indicate a command is being used.</param>
		/// <param name="sourceChannelId">Optional id for clients that require id's to send to channels. Id of the channel the message originated from.</param>
		public static void RaiseClientMessageReceived(object sender, TCRClientUser user, string msg, string commandPrefix, ulong sourceChannelId = 0)
			=> RaiseClientMessageReceived(sender, user, "", msg, commandPrefix, sourceChannelId);

		/// <summary>
		/// Emits a message to TerrariaChatRelay that a client message have been received.
		/// </summary>
		/// <param name="sender">Object that is emitting this event.</param>
		/// <param name="user">User object detailing the client source and username</param>
		/// <param name="msg">Text content of the message</param>
		/// <param name="commandPrefix">Command prefix to indicate a command is being used.</param>
		/// <param name="clientPrefix">String to insert before the main chat message.</param>
		/// <param name="sourceChannelId">Optional id for clients that require id's to send to channels. Id of the channel the message originated from.</param>
		public static void RaiseClientMessageReceived(object sender, TCRClientUser user, string clientPrefix, string msg, string commandPrefix, ulong sourceChannelId = 0)
		{
			if(CommandServ.IsCommand(msg, commandPrefix))
			{
				var payload = CommandServ.GetExecutableCommand(msg, commandPrefix, user);
				msg = payload.Execute();
				((IChatClient)sender).HandleCommand(payload, msg, sourceChannelId);
			}
			else
			{
				NetHelpers.BroadcastChatMessageWithoutTCRFormattable($"{clientPrefix}<{user.Username}> {msg}", -1);
				OnClientMessageReceived?.Invoke(sender, new ClientChatEventArgs(user, msg));
			}
		}

		/// <summary>
		/// Emits a message to all subscribers that a game message has been received with Color.White.
		/// </summary>
		/// <param name="sender">Object that is emitting this event.</param>
		/// <param name="playerId">Id of player in respect to Main.Player[i], where i is the index of the player.</param>
		/// <param name="msg">Text content of the message</param>
		public static void RaiseTerrariaMessageReceived(object sender, TCRPlayer player, string msg)
			=> RaiseTerrariaMessageReceived(sender, player, Color.White, msg);

		/// <summary>
		/// Emits a message to all subscribers that a game message has been received.
		/// </summary>
		/// <param name="sender">Object that is emitting this event.</param>
		/// <param name="playerId">Id of player in respect to Main.Player[i], where i is the index of the player.</param>
		/// <param name="color">Color to display the text.</param>
		/// <param name="msg">Text content of the message</param>
		public static void RaiseTerrariaMessageReceived(object sender, TCRPlayer player, Color color, string msg)
        {
            var snippets = ChatManager.ParseMessage(msg, color);

            string outmsg = "";
            foreach (var snippet in snippets)
            {
                outmsg += snippet.Text;
            }

            OnGameMessageReceived?.Invoke(sender, new TerrariaChatEventArgs(player, color, outmsg));
        }

		public static void ConnectClients()
        {
			PrettyPrint.Log("Connecting clients...");

			for (var i = 0; i < Subscribers.Count; i++)
			{
				PrettyPrint.Log(Subscribers[i].GetType().ToString() + " Connecting...");
				Subscribers[i].Connect();
            }
			Console.ResetColor();
        }

        public static void DisconnectClients()
        {
            foreach (var subscriber in Subscribers)
            {
                subscriber.Disconnect();
            }

			Subscribers.Clear();
        }
    }

    public class TerrariaChatEventArgs : EventArgs
    {
        public TCRPlayer Player { get; set; }
        public Color Color { get; set; }
        public string Message { get; set; }

		/// <summary>
		/// Message payload sent to subscribers when a game message has been received.
		/// </summary>
		/// <param name="player">Id of player in respect to Main.Player[i], where i is the index of the player.</param>
		/// <param name="color">Color to display the text.</param>
		/// 
		/// <param name="msg">Text content of the message</param>
		public TerrariaChatEventArgs(TCRPlayer player, Color color, string msg)
        {
			Player = player;
			Color = color;
            Message = msg;
		}
    }

	public class ClientChatEventArgs : EventArgs
	{
		public TCRClientUser User { get; set; }
		public string Message { get; set; }

		/// <summary>
		/// Message payload sent to subscribers when a game message has been received.
		/// </summary>
		/// <param name="player">Id of player in respect to Main.Player[i], where i is the index of the player.</param>
		/// <param name="color">Color to display the text.</param>
		/// <param name="msg">Text content of the message</param>
		public ClientChatEventArgs(TCRClientUser user, string msg)
		{
			User = user;
			Message = msg;
		}
	}
}
