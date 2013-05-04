using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.IO.Ports;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;


namespace WpfApplication1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {


        //Declaring the variables used for the application
        private string[] PortScannerHeaders = {"Protocol", "Local Address", "Foriegn Address", "State"};
        private string[] IPScannerHeaders = { "IP", "Ping", "Hostname", "Mac Address" };
        List<object> rows = new List<object>();
        List<object> result = new List<object>();
        private ProgressBarWin pbw = null;
        private Thread progress = null;
        private String errorMessage = "";


        //Main method to intailise the mainWindow Page to show up all the componants on the screen
        public MainWindow()
        {
            InitializeComponent();

            createTable(IPScannerHeaders);
        }


        //when the Start button is click this method is called.
        private void start_button_Click(object sender, RoutedEventArgs e)
        {
            //gets the Selected item in the Drop down box
            ComboBoxItem cbi = (ComboBoxItem)comboBox1.SelectedItem;
            //converting it to type string
            string val = cbi.Content.ToString();
            //Intialising variable row to a new fresh list to be used.
            rows = new List<object>();
            //does a check on the selected item to perform a task.
            if (val == "IP scanner")
            {
                //if its the IP scanner thats the selected item then it goes to the find method
                Find();

                //this is the progress bar window. When the find method id finished then it will close the progress bar window.
                //Checks to see if its on first befor turning it off!!
                if(progress != null)
                    progress.Abort();
            }
            else
            {
                //If the selected item is port scanner then it goes to this.
                //it calls the progress start method.
                progressStart();

                //calls the method getCommandResults and it returns a list and puts in in the variable list results
                List<string> results = getGetCommandResults("netstat", "-a");

                // It then gets this list and passes the variable to the method addToTable()
                addToTable(results);

                //turns off the progress bar window
                progress.Abort();
            }
        }


        public void Find()
        {
            // checks to see that the text boxes arent empty. if no it goes to the else statement below.
            if (TextBox1.Text != "" && TextBox2.Text != "")
            {
                // checks to see that the inputted ip addreses are correct. passes them through the method 
                // ipaddressCheck aand it performs a regulaer expression test on it.
                if (ipaddressCheck(TextBox1.Text, TextBox2.Text) == "")
                {
                    //starts the progress bar window
                    progressStart();

                    //gets the index of the last dot in the string
                    int lastF = TextBox1.Text.LastIndexOf(".");
                    //does the same
                    int lastT = TextBox2.Text.LastIndexOf(".");

                    //extracts after the last dot in the string. this will be the last port digits range
                    string frm = TextBox1.Text.Substring(lastF + 1);
                    string to = TextBox2.Text.Substring(lastT + 1);

                    //goes through the range of ports on the ip addresses
                        for (int i = int.Parse(frm); i <= int.Parse(to); i++)
                        {
                            // this gets the text before the last dot and then adds on the port number. eg.. 192.168.1 + .13
                            string address = TextBox2.Text.Substring(0, lastT + 1);

                            // creates a new class Ping. A class thats already avilible in c# library
                            Ping p = new Ping();

                            //this Trys to perform the certain task, but if it cant it will go to the catch statement below.
                            try
                            {

                                //this sends out a ping statement with the ipaddress
                                PingReply reply = p.Send(address + i, 1000);

                                // this checks to see if the reply comes back a success
                                if (reply.Status == IPStatus.Success)
                                {

                                    // does the same as above
                                    try
                                    {
                                        // gets the ip host entry by passing in the ip address
                                        IPHostEntry ip = Dns.GetHostByAddress(address + i);

                                        //creates an array of type string. This is a couple of texts in one container indexed to find them'
                                        string[] values = new string[4];

                                        // the ip address in the first indexed string 
                                        values[0] = address + i;
                                        //puts it in the second and so on
                                        values[1] = reply.RoundtripTime.ToString() + "ms";
                                        values[2] = ip.HostName;
                                        
                                        values[3] = GetMac(address + i);   //GetMac(address + i);


                                        //add the array values to the list rows to be put in to the table.
                                        rows.Add(values);

                                    }
                                        // this is the catch if the try fails
                                    catch (Exception)
                                    {
                                    }
                                }
                                    // this is the else for if the ping replay fails. means there is port open on that ip.
                                else
                                {
                                    //same as above
                                    string[] values = new string[4];
                                    values[0] = address + i;
                                    values[1] = reply.RoundtripTime.ToString() + "ms";
                                    values[2] = "";
                                    values[3] = "";

                                    rows.Add(values);
                                }
                            }

                            catch (PingException ex)
                            {
                            }
                    }
                        // when its finished the loop of going through the ip range it then adds the list of rows to the datagrid table
                        datagrid.ItemsSource = rows;
                }

                 // if the regular expression fails then this message appears in a dialog box with the error message.
                else
                {
                    MessageBox.Show(errorMessage);
                }
            }
                // if the textboxes are empty then a dialog box appears noting it.
            else
            {
                // this sets up the dialog box
                MessageBox.Show("Please enter in Value Range in To and From");
            }
        }


        // this is a method executes commands on the ecommand prompt in the background. takes two paramaters eg.. (netstat) (-a)
        private List<string> getGetCommandResults(string command, string param)
        {
            List<string> results = new List<string>();

            //all this sets up the command prompt and passes the commands into it.
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = command;
            psi.Arguments = param;
            psi.CreateNoWindow = true;
            psi.ErrorDialog = false;
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.WindowStyle = ProcessWindowStyle.Hidden;

            Process process = new Process();
            process.StartInfo = psi;

             //the results received from the command are put into the results variable
            process.ErrorDataReceived += (s, e) => { results.Add(e.Data); };
            process.OutputDataReceived += (s, e) => { results.Add(e.Data); };

            process.Start();

            process.BeginErrorReadLine();
            process.BeginOutputReadLine();


            //closes the command prompt when finished.
            process.WaitForExit();

            // it the then returns the results variable to whatever calls  this method
            return results;
        }


        // adds rows to the table.
        private void addToTable(List<string> vars)
        {

            //checks to see that there is more that 4 rows in the vars list.
            if (vars.Count > 4)
            {
                //goes through the vars list by doing a for loop. it starts off on index four because the first 3 lines
                //are unnesscary lines
                for (int i = 4; i < vars.Count(); i++)
                {
                    //checks to see that the number of index its on in the loop is not null
                    if (vars[i] != null)
                    {

                        int position = 0;
                        string[] param = new string[4];

                        //splits the text into words by by splitting them at spaces
                        string[] words = vars[i].Split(' ');

                        //goes through the list of words splitted
                        foreach (string w in words)
                        {
                            //checks to see if the word is not equal to null
                            if (w != "")
                            {
                                //if it isnt null then it puts it in the array variable param 
                                param[position] = w;

                                //increments the position of the array by 1
                                position++;
                            }
                        }

                        //it then adds the array to the list of rows.
                        rows.Add(param);
                    }
                }

                //clears the table of previous values
                datagrid.ItemsSource = "";

                //adds the new list of rows to the table
                this.datagrid.ItemsSource = rows;
            }
        }
        
       private string GetMac(String ip)
        {
            List<string> results = getGetCommandResults("nbtstat", "-a "+ip);
            string compMac = "hhh";
            if (results != null)
            {
                foreach (string val in results)
                {
                    if (val.Contains("MAC Address"))
                        compMac = val;
                }
            }
            return compMac;
        }

        //This is a listening method. everytime a new value is selected on the dropdown list it calls this method
        private void comboBox1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //gets the selected item
            ComboBoxItem cbi = (ComboBoxItem)comboBox1.SelectedItem;

            //converts to a string
            string val = cbi.Content.ToString();

            //does a check to see if its ip scanned or port scanner selected
            if (val == "IP scanner")
            {
                //calls the method to set the headers on the table
                createTable(IPScannerHeaders);
            }
            else
            {
                //calls the method to set the headers on the table
                createTable(PortScannerHeaders);
            }
        }


        //this method sets up the table and creates the headers
        private void createTable(string[] headers)
        {
            //checks to see if the datagrid isnt null
            if (datagrid != null)
            {
                //if the table has vlues it empties it.
                datagrid.ItemsSource = "";
                //refreshs the variable list back to new intialisation
                rows = new List<object>();
                //clears previous columns on the datagrid
                datagrid.Columns.Clear();
                //goes through the array list of headers pass through this method paramater and does a for loop
                for (int i = 0; i < headers.Length; i++)
                {
                    //creates a column variable 
                    var col = new DataGridTextColumn();
                    //adds the header column from the index = i on the headers array on the for loop
                    col.Header = headers[i];

                    //indexes the column for easy look up
                    col.Binding = new Binding("[" + i.ToString() + "]");

                    //adds the new column to the datagrid
                    datagrid.Columns.Add(col);
                }
            }
        }


        // the method that starts the progress bar window 
        private void progressStart()
        {
            //starts a new thread on the cpu. like a different application running on its own away from this one
            progress = new Thread(() =>
            {
                //create calls the ProgressBarWin class created in this project.
                pbw = new ProgressBarWin();
                //show up the progress bar window
                pbw.ShowDialog();
            });

            //sets the state of the thread
            progress.SetApartmentState(ApartmentState.STA);
            //starts running the thread.
            progress.Start();
        }

        private void tracert_button_Click(object sender, RoutedEventArgs e)
        {

        }

        //this is the regular expression used to check that the ip address is correct. pass in the two text box values
        private string ipaddressCheck(String ip1, string ip2)
        {
            //sets the string variable errormessage to null. declared at the top of this class.
            errorMessage = "";

            //regular expression to check the ip addresses passed in.
            Match result1 = Regex.Match(ip1, @"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}");
            Match result2 = Regex.Match(ip2, @"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}");

            //checks to see if there not valid.
            if (!result1.Success || !result2.Success)
            {
                //if not it sends back an error message.
                errorMessage = "IP address isnt valid. Please enter proper format xxx.xxx.xxx.xxx";
            }

            return errorMessage;
        }



    }
}