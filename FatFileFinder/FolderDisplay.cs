﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace FatFileFinder
{
    class FolderDisplay
    {
        public DataGrid dg;
        public FolderData folderData;
        public int level;
        public static int maxLevel;

        //event for UI
        public event EventHandler GridClicked;
        public event EventHandler GridSingleClick;
        public event EventHandler GridKeys;
        /*
         * Constructor
         * @param fd: FolderData object to display
         * @param l: level of the FolderData
         * */
        public FolderDisplay(FolderData fd, int l)
        {
            //setup grid
            folderData = fd;
            level = l;

            //set maxlevel
            if (level > maxLevel)
            {
                maxLevel = level;
            }

            //setup datagrid
            dg = new DataGrid() { IsReadOnly = true, GridLinesVisibility=DataGridGridLinesVisibility.Vertical, VerticalGridLinesBrush = new SolidColorBrush(Color.FromArgb(255,230,230,230)) /*AutoGenerateColumns = false*/};
            dg.DataContext = new Binding() { RelativeSource=RelativeSource.Self};

            //click handlers
            dg.MouseDoubleClick += cellClicked;
            dg.MouseLeftButtonUp += cellSingleClick;
            dg.KeyUp += keyPressed;

            updateTableListing();

            /* //Name column
             DataGridTextColumn header = new DataGridTextColumn() { Header = "Name" };
             header.Binding = new Binding("Name");
             dg.Columns.Add(header);

             //percentage column
             DataGridTemplateColumn percentage = new DataGridTemplateColumn() { Header="Percentage"};
             //percentage.CellTemplate = Application.Current.Resources["ProgressTemplate"] as DataTemplate;
             dg.Columns.Add(percentage);

             //Size column
             DataGridTextColumn size = new DataGridTextColumn() { Header = "Size" };
             header.Binding = new Binding("Size");
             dg.Columns.Add(size);*/
        }

        /* Handles Keyboard input in the datagrid
         * Supported keys: Arrows, Enter
         */
        private void keyPressed(object sender, RoutedEventArgs e)
        {
            KeyEventArgs ke = (KeyEventArgs)e;

            //if up or down arrow, datagrid automatically moves the selected cell, so only need to call the mouse
            //click event
            if (ke.Key == Key.Up || ke.Key == Key.Down)
            {
                cellSingleClick(sender, e);
            }
            //right arrow = doubleclick
            else if (ke.Key == Key.Right)
            {
                cellClicked(sender, e);
                //Make other datagrid the focus
                GridKeys(this,e);
            }
            //left arrow = backtrack
            else if (ke.Key == Key.Left)
            {
                //transfer keyboard input to the other datagrid
                GridKeys(this, e);
            }
            //enter key = open folder
            else if (ke.Key == Key.Enter)
            {
                GridKeys(this,e);
            }

        }

        //when a cell in this grid is double-clicked
        private void cellClicked(object sender, RoutedEventArgs e)
        {
            //it has to be a row
            DataGrid grid = (DataGrid)sender;

            if (grid.SelectedCells.Count < 1)
            {
                return;
            }

            //get the name cell
            dataEntry de = (dataEntry)(grid.SelectedCells[0].Item);

            //get the name only
            string nName = de.Name.Substring(3);

            //call out to update the UI (using a raised notification)

            try
            {
               //is it a folder?
               FolderData fd = folderData.sfDict[nName];
                try
                {
                    GridClicked(this, new FolderDisplayEvent() { folderData = fd });
                }
                catch (NullReferenceException)
                {
                    //silently catch
                }
            }
            catch (System.Collections.Generic.KeyNotFoundException){
                //it's a file, signal to show properties

                try
                {
                    //make sure that it's in there
                   
                    FileInfo fi = folderData.fDict[nName];
                    GridClicked(this, new FolderDisplayEvent() { fileInfo = fi });
                }
                catch (Exception)
                {
                    //it's a folder that hasn't been loaded yet

                }
            }          
        }

        //called on single click events
        private void cellSingleClick(object sender, RoutedEventArgs e)
        {
            //it has to be a row
            DataGrid grid = (DataGrid)sender;

            //make sure stuff is selected
            if (grid.SelectedCells.Count < 1)
                return;
           
            //get the name cell
            dataEntry de = (dataEntry)(grid.SelectedCells[0].Item);
            
         
            //get the name only
            string nName = de.Name.Substring(3);

            //call out to update the UI (using a raised notification)

            try
            {
                //is it a folder?
                FolderData fd = folderData.sfDict[nName];
                try
                {
                    GridSingleClick(this, new FolderDisplayEvent() { folderData = fd });
                }
                catch (NullReferenceException)
                {
                    //silently catch
                }
            }
            catch (System.Collections.Generic.KeyNotFoundException)
            {
                //it's a file, signal to show properties

                try
                {
                    //make sure that it's in there

                    FileInfo fi = folderData.fDict[nName];
                    GridSingleClick(this, new FolderDisplayEvent() { fileInfo = fi });
                }
                catch (Exception)
                {
                    //it's a folder that hasn't been loaded yet

                }
            }
        }


        //Takes a FolderData and polulates a DataGrid passed in
        public void updateTableListing()
        {
            List<dataEntry> lde = new List<dataEntry>();
            //add the Files
            foreach (System.IO.FileInfo file in folderData.files)
            {
                lde.Add(new dataEntry() { Name = "📝 " + file.Name, Percentage = Math.Round(file.Length / folderData.total_size,4) * 100, Size = formatSize(file.Length) });
            }
           
            //add the folders
            foreach (FolderData f in folderData.subFolders)
            {

                lde.Add(new dataEntry() { Name = "📁 " + f.path.Name, Percentage = Math.Round(f.total_size / folderData.total_size,4) * 100, Size = formatSize(f.total_size) });
            }

            //add folders that still need to be sized
            foreach (DirectoryInfo di in folderData.sfdi)
            {
                if (!folderData.sfDict.ContainsKey(di.Name))
                {
                    lde.Add(new dataEntry() { Name = "📁 " + di.Name + " (sizing)", Percentage = -1, Size = "Unknown" });
                }
            }

            dg.ItemsSource = lde;
        }

        //takes a file size and converts it to the best size unit
        public static string formatSize(double rawSize)
        {
            string formatted = "";
            int size = 1024;
            string[] suffix = new string[] { " bytes", " KB", " MB", " GB", " TB" };

            for (int i = 0; i < suffix.Length; i++)
            {
                double compare = Math.Pow(size, i);
                if (rawSize <= compare)
                {
                    int minus = 0;
                    if (i > 0)
                    {
                        minus = 1;
                    }
                    formatted = Math.Round(rawSize / Math.Pow(size, i - minus), 2) + suffix[i - minus];
                    break;
                }
            }

            return formatted;
        }

        public string totalSizeFormatted()
        {
            return formatSize(folderData.total_size);
        }
    }

    class FolderDisplayEvent : EventArgs
    {
        public FolderData folderData;
        public FileInfo fileInfo;
    }
}