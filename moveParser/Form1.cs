﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using hap = HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading;

namespace moveParser
{
    public partial class Form1 : Form
    {
        public class LevelUpMove
        {
            public int Level;
            public string Move;
        }
        public class MonData
        {
            public string VarName;
            public string DefName;
            public List<LevelUpMove> LevelMoves;
            public List<string> ExtraMoves;
        }

        public Form1()
        {
            InitializeComponent();
        }

        protected Dictionary<string, string> LoadPkmnNameListFromSerebii()
        {
            Dictionary<string, string> pkmnList = new Dictionary<string, string>();
            string html = "https://www.serebii.net/pokemon/nationalpokedex.shtml";

            hap.HtmlWeb web = new hap.HtmlWeb();
            hap.HtmlDocument htmlDoc = web.Load(html);
            hap.HtmlNodeCollection nodes = htmlDoc.DocumentNode.SelectNodes("//table[@class='dextable']/tr");

            for(int i = 2; i < nodes.Count; i++)
            {
                hap.HtmlNode nodo = nodes[i];
                string number = nodo.ChildNodes[1].InnerHtml.Trim().Replace("#", "");
                string species = nodo.ChildNodes[5].ChildNodes[1].InnerHtml.Trim();
                pkmnList.Add(number, species);
            }

            return pkmnList;
        }

        protected MonData LoadMonData(string number, string name, bool gen8)
        {
            MonData mon = new MonData();
            mon.DefName = "SPECIES_" + NameToDefineFormat(name);
            mon.VarName = NameToVarFormat(name);

            List<LevelUpMove> lvlMoves = new List<LevelUpMove>();
            List<string> ExtraMoves = new List<string>();

            string pokedex, identifier, lvlUpTitle, lvlUpTitle2, lvlUpTitle3, tmHmTrTitle;
            if (gen8)
            {
                pokedex = "-swsh";
                identifier = name.ToLower() + "/index";
                lvlUpTitle = "Standard Level Up";
                lvlUpTitle2 = "Standard Level Up";
                lvlUpTitle3 = "Standard Level Up";
                tmHmTrTitle = "TM & HM Attacks";
            }
            else
            {
                pokedex = "-sm";
                identifier = number;
                lvlUpTitle = "Generation VII Level Up";
                lvlUpTitle2 = "Ultra Sun/Ultra Moon Level Up";
                lvlUpTitle3 = "Standard Level Up";
                tmHmTrTitle = "TM & HM Attacks";
            }

            string html = "https://serebii.net/pokedex" + pokedex + "/" + identifier + ".shtml";

            hap.HtmlWeb web = new hap.HtmlWeb();
            hap.HtmlDocument htmlDoc = web.Load(html);
            hap.HtmlNodeCollection nodes = htmlDoc.DocumentNode.SelectNodes("//table[@class='dextable']/tr/td/h3");

            if (nodes != null)
            {
                for (int i = 0; i < nodes.Count; i++)
                {
                    hap.HtmlNodeCollection moves;
                    hap.HtmlNode nodo = nodes[i];
                    if (nodo.InnerText.Equals(lvlUpTitle) || nodo.InnerText.Equals(lvlUpTitle2) || nodo.InnerText.Equals(lvlUpTitle3))
                    {
                        moves = nodo.ParentNode.ParentNode.ParentNode.ChildNodes;
                        int move_num = 0;
                        string move_lvl;
                        foreach (hap.HtmlNode move in moves)
                        {
                            LevelUpMove lmove = new LevelUpMove();
                            if (move_num % 3 == 2)
                            {
                                move_lvl = move.ChildNodes[1].InnerText;
                                if (move_lvl.Equals("&#8212;"))
                                    lmove.Level = 1;
                                else if (move_lvl.Equals("Evolve"))
                                    lmove.Level = 0;
                                else
                                    lmove.Level = int.Parse(move_lvl);
                                lmove.Move = "MOVE_" + NameToDefineFormat(move.ChildNodes[3].ChildNodes[0].InnerText);

                                lvlMoves.Add(lmove);
                            }
                            move_num++;
                        }
                    }
                    else if (nodo.InnerText.Equals(tmHmTrTitle))
                    {
                        moves = nodo.ParentNode.ParentNode.ParentNode.ChildNodes;
                        int move_num = 0;
                        foreach (hap.HtmlNode move in moves)
                        {
                            if (move_num % 3 == 2)
                            {
                                //ExtraMoves.Add("MOVE_" + NameToDefineFormat(move.ChildNodes[2].ChildNodes[0].InnerText));
                            }
                            move_num++;
                        }
                    }
                }
            }
            mon.LevelMoves = lvlMoves;
            mon.ExtraMoves = ExtraMoves;

            return mon;
        }

        private string NameToDefineFormat(string oldname)
        {
            if (oldname.Equals("Nidoran&#9792;"))
                return "NIDORAN_F";

            return oldname.ToUpper().Replace(" ", "_").Replace("-", "_");
        }

        private string NameToVarFormat(string oldname)
        {
            if (oldname.Equals("Nidoran&#9792;"))
                return "NidoranF";

            string[] str = oldname.Replace(" ", "_").Replace("-", "_").Split('_');
            string final = "";
            foreach (string s in str)
                final += s;

            return final;
        }

        private void btnLoadFromSerebii_Click(object sender, EventArgs e)
        {
            backgroundWorker1.RunWorkerAsync();
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            List<MonData> Database = new List<MonData>();

            UpdateLoadingMessage("Loading species...");
            Dictionary<string, string> nameList = LoadPkmnNameListFromSerebii();

            int namecount = nameList.Count;

            int i = 1;
            foreach (KeyValuePair<string, string> item in nameList)
            {
                //if (i < 10)
                {
                    Database.Add(LoadMonData(item.Key, item.Value, checkBox1.Checked));
                }
                backgroundWorker1.ReportProgress(i * 100 / namecount);
                // Set the text.
                UpdateLoadingMessage(i.ToString() + " out of " + namecount + " Pokémon loaded.");
                i++;
            }

            File.WriteAllText("output/db.json", JsonConvert.SerializeObject(Database, Formatting.Indented));
        }

        private void backgroundWorker1_ProgressChanged(object sender,
            ProgressChangedEventArgs e)
        {
            // Change the value of the ProgressBar to the BackgroundWorker progress.
            pbar1.Value = e.ProgressPercentage;
            // Set the text.
            //this.Text = e.ProgressPercentage.ToString();
        }

        public void UpdateLoadingMessage(string newMessage)
        {
            this.Invoke((MethodInvoker)delegate {
                this.lblLoading.Text = newMessage;
            });
        }
    }
}
