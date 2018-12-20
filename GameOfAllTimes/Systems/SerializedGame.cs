using MagiCave.Core;
using System;
using RogueSharp;
using System.Xml.Serialization;
using System.Xml;
using RLNET;
using System.Collections.Generic;
using RogueSharp.Random;
using MagiCave.Interfaces;
using MagiCave.Items;

namespace MagiCave.Systems
{
    [Serializable]
    [XmlRoot]
    public class SerializedGame : IXmlSerializable
    {

        [XmlElement(ElementName = "TimeSpan")]
        public TimeSpan ts;

        [XmlElement(ElementName = "_steps")]
        public int steps;

        [XmlElement(ElementName = "_mapLevel")]
        public int mapLevel;

        [XmlElement(ElementName = "Player")]
        public Player Player;

        [XmlElement(ElementName = "MonstersOnLevel")]
        public Monster[][] MonstersOnLevel;

        //[XmlElement(ElementName = "Rooms")]
        //public Rectangle[][] Rooms;

        [XmlElement(ElementName = "Random")]
        public RandomState Random;
        
        [XmlElement(ElementName = "Levels")]
        public MapState[] Levels;

        [XmlElement(ElementName = "Doors")]
        public Door[][] Doors;

        public SerializedGame(bool check)
        {
            if (check == true)
            {
                Levels = new MapState[Game.Levels.Count];
                MonstersOnLevel = new Monster[Game.Levels.Count][];
                //Rooms = new Rectangle[Game.Levels.Count][];
                Doors = new Door[Game.Levels.Count][];
                int i = 0;
                foreach (DungeonMap map in Game.Levels)
                {
                    int j = 0;
                    MonstersOnLevel[i] = new Monster[map.Monsters.Count];
                    //Rooms[i] = new Rectangle[map.Rooms.Count];
                    Doors[i] = new Door[map.Doors.Count];
                    foreach (Door d in map.Doors)
                    {
                        Doors[i][j] = d;
                        j++;
                    }
                    j = 0;
                    //foreach (Rectangle room in map.Rooms)
                    //{
                    //    Rooms[i][j] = room;
                    //    j++;
                    //}
                    j = 0;
                    foreach (Monster monster in map.Monsters)
                    {
                        MonstersOnLevel[i][j] = monster;
                        j++;
                    }
                    Levels[i] = map.Save();
                    j = 0;
                    // Current version of Roguesharp doesn't save 
                    // IsExplored property in serialization process
                    // so IsExplored property is being added manually
                    foreach (ICell cell in map.GetAllCells())
                    {
                        if (cell.IsExplored)
                        {
                            Levels[i].Cells[j] |= MapState.CellProperties.Explored;
                        }
                        j++;
                    }
                    i++;
                }
                Player = Game.Player;
                Random = Game.Random.Save();
                ts = Game.ts;
                mapLevel = Game.mapLevel;
                steps = Game.steps;
            }
        }
        
        public System.Xml.Schema.XmlSchema GetSchema() { return null; }

        public void ReadXml(XmlReader reader)
        {
            reader.MoveToContent();

            if (reader.IsStartElement("Game"))
            {
                reader.ReadStartElement("Game");
                ts = TimeSpan.ParseExact(reader.ReadElementString("Time"), "c", null);
                
                steps = Convert.ToInt32(reader.ReadElementString("_steps"));
                
                mapLevel = Convert.ToInt32(reader.ReadElementString("_mapLevel"));

                XmlSerializer RandDesializer = new XmlSerializer(typeof(RandomState));
                Random = (RandomState)RandDesializer.Deserialize(reader);

                Player = ReadPlayer(reader);

                int TotalLevels = Convert.ToInt32(reader.ReadElementString("TotalLevels"));
                Levels = new MapState[TotalLevels];
                //Rooms = new Rectangle[TotalLevels][];
                MonstersOnLevel = new Monster[TotalLevels][];
                Doors = new Door[TotalLevels][];
                XmlSerializer deserializer = new XmlSerializer(typeof(MapState));
                XmlSerializer ReckDesializer = new XmlSerializer(typeof(Rectangle));
                for (int i = 0; i < TotalLevels; i++)
                {
                    reader.ReadStartElement("DungeonMapLevel", i.ToString());
                    Levels[i] = (MapState)deserializer.Deserialize(reader);
                    //// Loading all rectangles on the level
                    //reader.ReadStartElement("Rooms");
                    //int AmOfRooms = Convert.ToInt32(reader.ReadElementString("Amount"));
                    ////Rooms[i] = new Rectangle[AmOfRooms];
                    //for (int j = 0; j < AmOfRooms; j++)
                    //{
                    //    //Rooms[i][j] = (Rectangle)ReckDesializer.Deserialize(reader);
                    //}
                    //reader.ReadEndElement();
                    // Loading all doors on the level
                    reader.ReadStartElement("Doors");
                    int AmOfDoors = Convert.ToInt32(reader.ReadElementString("Amount"));
                    Doors[i] = new Door[AmOfDoors];
                    for (int j = 0; j < AmOfDoors; j++)
                    {
                        Doors[i][j] = new Door()
                        {
                            X = Convert.ToInt32(reader.ReadElementString("X")),
                            Y = Convert.ToInt32(reader.ReadElementString("Y")),
                            BackgroundColor = new RLColor((float)Convert.ToDouble(reader.ReadElementString("r")),
                                                          (float)Convert.ToDouble(reader.ReadElementString("g")),
                                                          (float)Convert.ToDouble(reader.ReadElementString("b"))),
                            Color = new RLColor((float)Convert.ToDouble(reader.ReadElementString("r")),
                                               (float)Convert.ToDouble(reader.ReadElementString("g")),
                                               (float)Convert.ToDouble(reader.ReadElementString("b"))),
                            IsOpen = Convert.ToBoolean(reader.ReadElementString("IsOpen")),
                            Symbol = reader.ReadElementString("Symbol").ToCharArray()[0]
                        };
                    }
                    reader.ReadEndElement();
                    int AmountOfMonsters = Convert.ToInt32(reader.ReadElementString("AmountOfMonsters"));
                    MonstersOnLevel[i] = new Monster[AmountOfMonsters];
                    for (int j = 0; j < AmountOfMonsters; j++)
                    {
                        MonstersOnLevel[i][j] = ReadMonster(reader);
                        if (MonstersOnLevel[i][j].Name == "Kobold")
                        {
                            MonstersOnLevel[i][j] = new Monsters.Kobold(MonstersOnLevel[i][j]);
                        }
                        else if (MonstersOnLevel[i][j].Name == "Dragon")
                        {
                            MonstersOnLevel[i][j] = new Monsters.Dragon(MonstersOnLevel[i][j]);
                        }
                    }
                    reader.ReadEndElement();
                }
                reader.ReadEndElement();
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartDocument();

            writer.WriteStartElement("Game");
            
            writer.WriteElementString("Time", ts.ToString());

            writer.WriteElementString("_steps", steps.ToString());
            
            writer.WriteElementString("_mapLevel", mapLevel.ToString());

            XmlSerializer RandSerializer = new XmlSerializer(typeof(RandomState));
            RandSerializer.Serialize(writer, Random);

            WriteActor(writer, Player);

            writer.WriteElementString("TotalLevels", Game.Levels.Count.ToString());
            
            XmlSerializer serializer = new XmlSerializer(typeof(MapState));
            XmlSerializer ReckSerializer = new XmlSerializer(typeof(Rectangle));
            for (int i = 0; i < Game.Levels.Count; i++)
            {
                writer.WriteStartElement("DungeonMapLevel", i.ToString());
                serializer.Serialize(writer, Levels[i]);

                //writer.WriteStartElement("Rooms");
                //writer.WriteElementString("Amount", Rooms[i].Length.ToString());
                //foreach (Rectangle r in Rooms[i])
                //{
                //    ReckSerializer.Serialize(writer, r);
                //}
                //writer.WriteEndElement();

                writer.WriteStartElement("Doors");
                writer.WriteElementString("Amount", Doors[i].Length.ToString());
                foreach (Door d in Doors[i])
                {
                    writer.WriteElementString("X", d.X.ToString());
                    writer.WriteElementString("Y", d.Y.ToString());
                    writer.WriteElementString("r", d.BackgroundColor.r.ToString());
                    writer.WriteElementString("g", d.BackgroundColor.g.ToString());
                    writer.WriteElementString("b", d.BackgroundColor.b.ToString());
                    writer.WriteElementString("r", d.Color.r.ToString());
                    writer.WriteElementString("g", d.Color.g.ToString());
                    writer.WriteElementString("b", d.Color.b.ToString());
                    writer.WriteElementString("IsOpen", d.IsOpen.ToString());
                    writer.WriteElementString("Symbol", d.Symbol.ToString());
                }
                writer.WriteEndElement();

                writer.WriteElementString("AmountOfMonsters", MonstersOnLevel[i].Length.ToString());
                foreach (Monster monster in MonstersOnLevel[i])
                {
                    WriteActor(writer, monster);
                }
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }

        private void WriteActor(XmlWriter writer, Actor actor)
        {
            if (actor is Player)
                writer.WriteStartElement("Player");
            else
                writer.WriteStartElement("Monster");
            writer.WriteElementString("Name", actor.Name);
            writer.WriteElementString("Attack", actor.Attack.ToString());
            writer.WriteElementString("AttackChance", actor.AttackChance.ToString());
            
            writer.WriteElementString("r", actor.Color.r.ToString());
            writer.WriteElementString("g", actor.Color.g.ToString());
            writer.WriteElementString("b", actor.Color.b.ToString());

            writer.WriteElementString("Awareness", actor.Awareness.ToString());
            writer.WriteElementString("Defense", actor.Defense.ToString());
            writer.WriteElementString("DefenseChance", actor.DefenseChance.ToString());

            writer.WriteElementString("Gold", actor.Gold.ToString());
            writer.WriteElementString("Health", actor.Health.ToString());
            writer.WriteElementString("MaxHealth", actor.MaxHealth.ToString());

            writer.WriteElementString("Size", actor.Size.ToString());
            writer.WriteElementString("Speed", actor.Speed.ToString());
            writer.WriteElementString("Symbol", actor.Symbol.ToString());
            
            writer.WriteElementString("X", actor.X.ToString());
            writer.WriteElementString("Y", actor.Y.ToString());
            if (actor is Player)
            {
                Player pl = (Player)actor;
                writer.WriteElementString("Kills", pl.Kills.ToString());
            }

            writer.WriteStartElement("Items");
            if (actor.Items.Count != 0)
            {
                foreach (IItem item in actor.Items)
                {
                    writer.WriteElementString("Item", item.ToString());
                }
            }
            else
            {
                writer.WriteElementString("Item", "null");
            }
            writer.WriteEndElement();

            writer.WriteStartElement("AreaControlled");
            foreach(ICell cell in actor.AreaControlled)
            {
                WriteICell(writer, cell);
            }
            writer.WriteEndElement();
            writer.WriteEndElement();
        }
        private Monster ReadMonster(XmlReader reader)
        {
            reader.ReadStartElement("Monster");
            Monster monster = new Monster
            {
                Name = reader.ReadElementString("Name"),
                Attack = Convert.ToInt32(reader.ReadElementString("Attack")),
                AttackChance = Convert.ToInt32(reader.ReadElementString("AttackChance")),
                Color = new RLColor((float)Convert.ToDouble(reader.ReadElementString("r")),
                                    (float)Convert.ToDouble(reader.ReadElementString("g")),
                                    (float)Convert.ToDouble(reader.ReadElementString("b"))),
                Awareness = Convert.ToInt32(reader.ReadElementString("Awareness")),
                Defense = Convert.ToInt32(reader.ReadElementString("Defense")),
                DefenseChance = Convert.ToInt32(reader.ReadElementString("DefenseChance")),
                Gold = Convert.ToInt32(reader.ReadElementString("Gold")),
                Health = Convert.ToInt32(reader.ReadElementString("Health")),
                MaxHealth = Convert.ToInt32(reader.ReadElementString("MaxHealth")),
                Size = Convert.ToInt32(reader.ReadElementString("Size")),
                Speed = Convert.ToInt32(reader.ReadElementString("Speed")),
                Symbol = reader.ReadElementString("Symbol").ToCharArray()[0],
                X = Convert.ToInt32(reader.ReadElementString("X")),
                Y = Convert.ToInt32(reader.ReadElementString("Y"))
            };
            if (reader.IsStartElement("Items"))
            {
                reader.ReadStartElement("Items");
                monster.Items = new List<IItem>();
                while (reader.IsStartElement("Item"))
                {
                    IItem item = ReadItem(reader);
                    if (item != null)
                    {
                        monster.Items.Add(item);
                    }
                }
                reader.ReadEndElement();
            }
            if (reader.IsStartElement("AreaControlled"))
            {
                reader.ReadStartElement("AreaControlled");
                monster.AreaControlled = new List<ICell>();
                while (reader.IsStartElement("Cell") || reader.IsStartElement("Tile"))
                {
                    monster.AreaControlled.Add(ReadICell(reader));
                }
                reader.ReadEndElement();
            }
            reader.ReadEndElement();
            return monster;
        }

        private Player ReadPlayer(XmlReader reader)
        {
            reader.ReadStartElement("Player");
            Player player = new Player
            {
                Name = reader.ReadElementString("Name"),
                Attack = Convert.ToInt32(reader.ReadElementString("Attack")),
                AttackChance = Convert.ToInt32(reader.ReadElementString("AttackChance")),
                Color = new RLColor((float)Convert.ToDouble(reader.ReadElementString("r")),
                                    (float)Convert.ToDouble(reader.ReadElementString("g")),
                                    (float)Convert.ToDouble(reader.ReadElementString("b"))),
                Awareness = Convert.ToInt32(reader.ReadElementString("Awareness")),
                Defense = Convert.ToInt32(reader.ReadElementString("Defense")),
                DefenseChance = Convert.ToInt32(reader.ReadElementString("DefenseChance")),
                Gold = Convert.ToInt32(reader.ReadElementString("Gold")),
                Health = Convert.ToInt32(reader.ReadElementString("Health")),
                MaxHealth = Convert.ToInt32(reader.ReadElementString("MaxHealth")),
                Size = Convert.ToInt32(reader.ReadElementString("Size")),
                Speed = Convert.ToInt32(reader.ReadElementString("Speed")),
                Symbol = reader.ReadElementString("Symbol").ToCharArray()[0],
                X = Convert.ToInt32(reader.ReadElementString("X")),
                Y = Convert.ToInt32(reader.ReadElementString("Y")),
                Kills = Convert.ToInt32(reader.ReadElementString("Kills"))
            };
            if (reader.IsStartElement("Items"))
            {
                reader.ReadStartElement("Items");
                player.Items = new List<IItem>();
                while (reader.IsStartElement("Item"))
                {
                    IItem item = ReadItem(reader);
                    if (item != null)
                    {
                        player.Items.Add(item);
                    }
                }
                reader.ReadEndElement();
            }
            if (reader.IsStartElement("AreaControlled"))
            {
                reader.ReadStartElement("AreaControlled");
                player.AreaControlled = new List<ICell>();
                while(reader.IsStartElement("Cell") || reader.IsStartElement("Tile"))
                {
                    player.AreaControlled.Add(ReadICell(reader));
                }
                reader.ReadEndElement();
            }
            reader.ReadEndElement();
            return player;
        }

        private IItem ReadItem(XmlReader reader)
        {
            if (reader.IsStartElement("Item"))
            {
                if (reader.ReadElementString("Item") == "Healing Potion")
                {
                    return new HealingPotion(0, 0);
                }
            }
            return null;
        }

        private ICell ReadICell(XmlReader reader)
        {
            if (reader.IsStartElement("Cell"))
            {
                reader.ReadStartElement("Cell");
                Cell cell = new Cell(Convert.ToInt32(reader.ReadElementString("X")),
                                     Convert.ToInt32(reader.ReadElementString("Y")),
                                     Convert.ToBoolean(reader.ReadElementString("IsTransparent")),
                                     Convert.ToBoolean(reader.ReadElementString("IsInFov")),
                                     Convert.ToBoolean(reader.ReadElementString("IsWalkable")),
                                     Convert.ToBoolean(reader.ReadElementString("IsExplored")));
                reader.ReadEndElement();
                return cell;
            }
            else
            {
                return null;
            }
        }

        private void WriteICell(XmlWriter writer, ICell cell)
        {
            if (cell is Cell)
                writer.WriteStartElement("Cell");
            else
                writer.WriteStartElement("Tile");
            writer.WriteElementString("X", cell.X.ToString());
            writer.WriteElementString("Y", cell.Y.ToString());
            writer.WriteElementString("IsTransparent", cell.IsTransparent.ToString());
            writer.WriteElementString("IsInFov", cell.IsInFov.ToString());
            writer.WriteElementString("IsWalkable", cell.IsWalkable.ToString());
            writer.WriteElementString("IsExplored", cell.IsExplored.ToString());
            writer.WriteEndElement();
        }
    }
}
