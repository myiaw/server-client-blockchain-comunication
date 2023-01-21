using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Newtonsoft.Json;
using server_client_blockchain_communication.Classes;

namespace server_client_blockchain_communication;

public partial class MainWindow{
    private const string IP_ADDRESS = "127.0.0.1";

    //For only allowing numbers in the text box.
    private static readonly Regex Regex = new("[^0-9.-]+");

    //Declare the blockchain.
    public static Blockchain _blockchain;

    //Declare the list of connected clients.
    private static readonly List<int> _peers = new();

    private static TcpListener tcpListener;
    private static TcpClient client;
    private static string Data;

    private static int PortNum;

    private Thread minerThread;
    private Thread syncThread;
    //Declare the threads


    public MainWindow() {
        InitializeComponent();
        ServicePointManager.SecurityProtocol |=
            SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
        //Initializing the blockchain -> Genesis created in constructor.
        _blockchain = new Blockchain();
        //Whenever a new valid block is added to chainUI, update the UI.
        _blockchain._chainUI.CollectionChanged += ChainUI_CollectionChanged;

        var usedPorts = new List<int>();

        // Start the server in a separate thread
        Task.Factory.StartNew(() => {
            // Set the IP address and port number for the server
            var ipAddress = IPAddress.Parse("127.0.0.1");
            var r = new Random();
            var rInt = r.Next(1236, 1300);
            if (usedPorts.Contains(rInt)) rInt = r.Next(1236, 1300);
            usedPorts.Add(rInt);
            var port = rInt;
            PortNum = port;

            // Create a new TCP listener
            var tcpListener = new TcpListener(ipAddress, port);

            // Start the TCP listener
            tcpListener.Start();

            // Output a message to the user
            Dispatcher.Invoke(() => OutputMining.Text = "Server started. Waiting for client to connect...\n");
            Dispatcher.Invoke(() => StatusLabel.Content = "ONLINE: " + port);
            // Wait for a client to connect
            var client = tcpListener.AcceptTcpClient();

            // Output a message to the user
            Dispatcher.Invoke(() => OutputMining.Text += "Client connected.\n");
        });
    }


    private static void GetMeABlock() {
        while (true) {
            var block = _blockchain.Mine();
            _blockchain.AddToChain(block);
        }
    }


    /// If the text is not a match for the regular expression, then it is allowed
    private static bool IsTextAllowed(string text) {
        return !Regex.IsMatch(text);
    }


    private void ConnectToServer(int portNumber) {
        try {
            // Create a new TcpClient and connect to the server using the specified port number
            client = new TcpClient();
            client.Connect("127.0.0.1", portNumber);
            
            // Output a message to the user
            OutputMining.Text += "Connected to server.\n";
        }
        catch (SocketException) {
            // Output an error message if the connection fails
            OutputMining.Text += "Error: Failed to connect to server.\n";
        }
    }


    private void PortBox_OnPreviewTextInput(object sender, TextCompositionEventArgs e) {
        e.Handled = !IsTextAllowed(e.Text);
    }

    private void ConnectToBlockchain_onClick(object sender, RoutedEventArgs e) {
        var portNumberString = PortBox.Text;

        // Parse the port number as an integer
        if (int.TryParse(portNumberString, out var portNumber)) {
            // Connect to the server using the port number
            ConnectToServer(portNumber);
            PortNum = portNumber;
        }
        else {
            // Output an error message if the port number is invalid
            OutputMining.Text += "Error: Invalid port number.\n";
        }
    }


    private void MineButton_OnClick(object sender, RoutedEventArgs e) {
        //We start a new thread when we start mining.
        minerThread = new Thread(GetMeABlock);
        minerThread.Start();
        Task.Run(SyncBlockchain);
        
        MineButton.IsEnabled = false;
    }


    private static async Task SyncBlockchain()
    {
        // Get the network stream for sending and receiving data
        using var stream = client.GetStream();

        // Use a loop to synchronize the blockchain at regular intervals
        while (true)
        {
            // Send the blockchain data
            await SendAsync(stream, _blockchain._chain);

            // Receive data from the server
            var receivedChain = await ReceiveAsync(stream);
            if (receivedChain != null)
            {
                if(receivedChain.Count > _blockchain._chain.Count) {
                    if(Blockchain.ValidateChain(receivedChain)) {
                        _blockchain._chain = receivedChain;
                    }
                }
            }
            // Wait for 5 seconds before synchronizing the blockchain again
            await Task.Delay(5000);
        }
    }

    private static async Task SendAsync(NetworkStream stream, List<Block> data)
    {
        var formatter = new BinaryFormatter();
        using var memoryStream = new MemoryStream();
        formatter.Serialize(memoryStream, data);
        memoryStream.Position = 0;
        await stream.WriteAsync(memoryStream.ToArray(), 0, (int)memoryStream.Length);
    }

    private static async Task<List<Block>> ReceiveAsync(NetworkStream stream)
    {
        var formatter = new BinaryFormatter();
        using (var memoryStream = new MemoryStream())
        {
            await stream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            return (List<Block>)formatter.Deserialize(memoryStream);
        }
    }


    private void ChainUI_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
        if (e.Action != NotifyCollectionChangedAction.Add) return;
        if (e.NewItems[0] is not Block block) return;
        try {
            if (block.ValidateBlock())
                Application.Current.Dispatcher.Invoke(
                    () => {
                        OutputMining.Foreground = Brushes.LightGreen;
                        OutputMining.Text = block.ToString();
                    });
            else
                Application.Current.Dispatcher.Invoke(
                    () => {
                        OutputMining.Foreground = Brushes.Red;
                        OutputMining.Text = block.ToString();
                    });
        }
        catch (Exception exception) {
            Console.WriteLine(@"Can't update UI: " + exception.Message);
        }
    }
}