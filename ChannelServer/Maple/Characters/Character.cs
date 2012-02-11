﻿using System;
using System.Collections.Generic;
using Loki.Data;
using Loki.IO;
using Loki.Maple.Commands;
using Loki.Maple.Data;
using Loki.Maple.Interaction;
using Loki.Maple.Life;
using Loki.Maple.Maps;
using Loki.Net;

namespace Loki.Maple.Characters
{
    public class Character : MapObject, ISpawnable
    {
        public ChannelClientHandler Client { get; private set; }

        public CharacterItems Items { get; private set; }
        public CharacterSkills Skills { get; private set; }
        public CharacterQuests Quests { get; private set; }
        public CharacterBuffs Buffs { get; private set; }
        public CharacterKeyMap KeyMap { get; set; }
        public CharacterBuddyList BuddyList { get; private set; }
        public ControlledMobs ControlledMobs { get; private set; }
        public ControlledNpcs ControlledNpcs { get; private set; }
        public Trade Trade { get; set; }
        public Omok Omok { get; set; }
        public PlayerShop PlayerShop { get; set; }
        public HiredMerchant HiredMerchant { get; set; }
        public Fredrick Fredrick { get; set; }

        public int ID { get; private set; }
        public int AccountID { get; set; }
        public string Name { get; set; }
        public bool IsInitialized { get; private set; }

        private DateTime LastHealHPOverTime = new DateTime();
        private DateTime LastHealMPOverTime = new DateTime();

        public Gender Gender { get; set; }
        public byte SpawnPoint { get; set; }
        public byte MaxBuddies { get; set; }
        public bool IsMaster { get; set; }
        public byte Stance { get; set; }

        private byte skin;
        private int face;
        private int hair;
        private byte level;
        private Job job;
        private short strength;
        private short dexterity;
        private short intelligence;
        private short luck;
        private short currentHP;
        private short maxHP;
        private short currentMP;
        private short maxMP;
        private short availableAP;
        private short availableSP;
        private int experience;
        private short fame;
        private int meso;
        private Npc lastNpc;

        public byte Skin
        {
            get
            {
                return skin;
            }
            set
            {
                if (!World.AvailableStyles.Skins.Contains(value))
                {
                    throw new StyleUnavailableException();
                }
                else
                {
                    skin = value;

                    if (this.IsInitialized)
                    {
                        this.UpdateStatistics(StatisticType.Skin);
                        this.UpdateLook();
                    }
                }
            }
        }

        public int Face
        {
            get
            {
                return face;
            }
            set
            {
                if (!(World.AvailableStyles.MaleFaces.Contains(value) || World.AvailableStyles.FemaleFaces.Contains(value)))
                {
                    throw new StyleUnavailableException();
                }
                else
                {
                    face = value;

                    if (this.IsInitialized)
                    {
                        this.UpdateStatistics(StatisticType.Face);
                        this.UpdateLook();
                    }
                }
            }
        }

        public int Hair
        {
            get
            {
                return hair;
            }
            set
            {
                if (!(World.AvailableStyles.MaleHairs.Contains(value) || World.AvailableStyles.FemaleHairs.Contains(value)))
                {
                    throw new StyleUnavailableException();
                }
                else
                {
                    hair = value;

                    if (this.IsInitialized)
                    {
                        this.UpdateStatistics(StatisticType.Hair);
                        this.UpdateLook();
                    }
                }
            }
        }

        public int HairStyleOffset
        {
            get
            {
                return (this.Hair / 10) * 10;
            }
        }

        public int FaceStyleOffset
        {
            get
            {
                return (this.Face - (10 * (this.Face / 10))) + (this.Gender == Gender.Male ? 20000 : 21000);
            }
        }

        public int HairColorOffset
        {
            get
            {
                return this.Hair - (10 * (this.Hair / 10));
            }
        }

        public int FaceColorOffset
        {
            get
            {
                return ((this.Face / 100) - (10 * (this.Face / 1000))) * 100;
            }
        }

        public byte Level
        {
            get
            {
                return level;
            }
            set
            {
                if (value > 200)
                {
                    throw new ArgumentException("Level cannot exceed 200.");
                }
                else
                {
                    int deltaLevel = value - this.Level;

                    if (!this.IsInitialized)
                    {
                        level = value;
                    }
                    else
                    {
                        if (deltaLevel < 0)
                        {
                            level = value;

                            this.UpdateStatistics(StatisticType.Level);
                        }
                        else
                        {
                            for (int i = 0; i < deltaLevel; i++)
                            {
                                // TODO: Improve HP / MP

                                level++;

                                if (this.job == Maple.Job.Beginner && Level < 11)
                                {
                                    if (this.Level < 6)
                                    {
                                        this.Strength += 5;
                                    }
                                    else
                                    {
                                        this.Strength += 4;
                                        this.Dexterity += 1;
                                    }
                                }
                                else if (this.job == Maple.Job.Beginner && Level == 11)
                                {
                                    this.ResetStats();
                                }
                                else
                                {
                                    this.AvailableAP += 5;

                                    if (this.IsCygnus && this.Level < 70)
                                    {
                                        this.AvailableAP++;
                                    }
                                }

                                if (this.Job != Job.Beginner && this.Job != Job.Noblesse)
                                {
                                    this.AvailableSP += 3;
                                }

                                this.UpdateStatistics(StatisticType.Level);

                                this.ShowEffect(ForeignEffect.LevelUp);
                            }
                        }

                        this.CurrentHP = this.MaxHP;
                        this.CurrentMP = this.MaxMP;
                    }
                }
            }
        }

        public Job Job
        {
            get
            {
                return job;
            }
            set
            {
                job = value;

                if (this.IsInitialized)
                {
                    //this.AvailableSP += 1; TODO: Look if like Evan/Aran have special handlings on these.

                    this.UpdateStatistics(StatisticType.Job);

                    this.ShowEffect(ForeignEffect.JobAdvance);
                }

                //this.Skills.DeleteJobSkills();
            }
        }

        public short Strength
        {
            get
            {
                return strength;
            }
            set
            {
                strength = value;

                if (this.IsInitialized)
                {
                    this.UpdateStatistics(StatisticType.Strength);
                }
            }
        }

        public short Dexterity
        {
            get
            {
                return dexterity;
            }
            set
            {
                dexterity = value;

                if (this.IsInitialized)
                {
                    this.UpdateStatistics(StatisticType.Dexterity);
                }
            }
        }

        public short Intelligence
        {
            get
            {
                return intelligence;
            }
            set
            {
                intelligence = value;

                if (this.IsInitialized)
                {
                    this.UpdateStatistics(StatisticType.Intelligence);
                }
            }
        }

        public short Luck
        {
            get
            {
                return luck;
            }
            set
            {
                luck = value;

                if (this.IsInitialized)
                {
                    this.UpdateStatistics(StatisticType.Luck);
                }
            }
        }

        public short CurrentHP
        {
            get
            {
                return currentHP;
            }
            set
            {
                if (value < 0)
                {
                    currentHP = 0;
                }
                else if (value > this.MaxHP)
                {
                    currentHP = this.MaxHP;
                }
                else
                {
                    currentHP = value;
                }

                if (this.IsInitialized)
                {
                    this.UpdateStatistics(StatisticType.CurrentHP);
                }
            }
        }

        public short MaxHP
        {
            get
            {
                return maxHP;
            }
            set
            {
                maxHP = value;

                if (this.IsInitialized)
                {
                    this.UpdateStatistics(StatisticType.MaxHP);
                }
            }
        }

        public short CurrentMP
        {
            get
            {
                return currentMP;
            }
            set
            {
                currentMP = value;

                if (this.IsInitialized)
                {
                    this.UpdateStatistics(StatisticType.CurrentMP);
                }
            }
        }

        public short MaxMP
        {
            get
            {
                return maxMP;
            }
            set
            {
                maxMP = value;

                if (this.IsInitialized)
                {
                    this.UpdateStatistics(StatisticType.MaxMP);
                }
            }
        }

        public short AvailableAP
        {
            get
            {
                return availableAP;
            }
            set
            {
                availableAP = value;

                if (this.IsInitialized)
                {
                    this.UpdateStatistics(StatisticType.AvailableAP);
                }
            }
        }

        public short AvailableSP
        {
            get
            {
                return availableSP;
            }
            set
            {
                availableSP = value;

                if (this.IsInitialized)
                {
                    this.UpdateStatistics(StatisticType.AvailableSP);
                }
            }
        }

        public int Experience
        {
            get
            {
                return experience;
            }
            set
            {
                int delta = value - experience;

                experience = value;

                if (ChannelServer.AllowMultiLeveling)
                {
                    while (experience >= ExperienceTables.CharacterLevel[this.Level])
                    {
                        experience -= ExperienceTables.CharacterLevel[this.Level];

                        this.Level++;
                    }
                }
                else
                {
                    if (experience >= ExperienceTables.CharacterLevel[this.Level])
                    {
                        experience -= ExperienceTables.CharacterLevel[this.Level];

                        this.Level++;
                    }

                    if (experience >= ExperienceTables.CharacterLevel[this.Level])
                    {
                        experience = ExperienceTables.CharacterLevel[this.Level] - 1;
                    }
                }

                if (this.IsInitialized && delta != 0)
                {
                    this.UpdateStatistics(StatisticType.Experience);

                    using (Packet outPacket = new Packet(MapleServerOperationCode.ShowStatusInfo))
                    {
                        outPacket.WriteByte(3); // 3 = exp, 4 = fame, 5 = mesos, 6 = guildpoints
                        outPacket.WriteBool(true); // White?
                        outPacket.WriteInt(delta);
                        outPacket.WriteByte();
                        outPacket.WriteInt(); // monster book bonus (Bonus Event Exp)
                        outPacket.WriteShort(); //Weird stuff
                        outPacket.WriteInt(); //wedding bonus
                        outPacket.WriteByte(); //0 = party bonus, 1 = Bonus Event party Exp () x0
                        outPacket.WriteInt(); // party bonus
                        outPacket.WriteInt(); //equip bonus
                        outPacket.WriteInt(); //Internet Cafe Bonus
                        outPacket.WriteInt(); //Rainbow Week Bonus

                        this.Client.Send(outPacket);
                    }
                }
            }
        }

        public short Fame
        {
            get
            {
                return fame;
            }
            set
            {
                fame = value;

                if (this.IsInitialized)
                {
                    this.UpdateStatistics(StatisticType.Fame);
                }
            }
        }

        public int Meso
        {
            get
            {
                return meso;
            }
            set
            {
                if (meso > 0 && value > 0 && meso + value < 0) // Overflow
                {
                    meso = int.MaxValue;
                }
                else if (value < 0)
                {
                    throw new ArgumentException("Invalid meso amount.");
                }
                else
                {
                    meso = value;
                }

                if (this.IsInitialized)
                {
                    this.UpdateStatistics(StatisticType.Meso);
                }
            }
        }

        public bool IsAlive
        {
            get
            {
                return this.CurrentHP > 0;
            }
        }

        public bool IsLoggedIn
        {
            get
            {
                return ChannelServer.LoggedIn.Contains(this.AccountID);
            }
            set
            {
                if (value && !ChannelServer.LoggedIn.Contains(this.AccountID))
                {
                    ChannelServer.LoggedIn.Add(this.AccountID);
                }
                else if (!value && ChannelServer.LoggedIn.Contains(this.AccountID))
                {
                    ChannelServer.LoggedIn.Remove(this.AccountID);
                }
            }
        }

        public bool IsCygnus
        {
            get
            {
                return (short)this.Job >= 1000;
            }
        }

        public bool IsExplorer
        {
            get
            {
                return (short)this.Job < 900;
            }
        }

        public new Map Map
        {
            get
            {
                if (this.IsInitialized)
                {
                    return base.Map;
                }
                else
                {
                    try
                    {
                        return World.Maps[Database.Fetch("characters", "MapID", "ID = '{0}'", this.ID)];
                    }
                    catch
                    {
                        return World.Maps[0];
                    }
                }
            }
            set
            {
                base.Map = value;
            }
        }

        public Portal ClosestPortal
        {
            get
            {
                Portal closestPortal = null;
                double shortestDistance = double.PositiveInfinity;

                foreach (Portal loopPortal in (this.Map.Portals))
                {
                    double distance = loopPortal.Position.DistanceFrom(this.Position);

                    if (distance < shortestDistance)
                    {
                        closestPortal = loopPortal;
                        shortestDistance = distance;
                    }
                }

                return closestPortal;
            }
        }

        public Portal ClosestSpawnPoint
        {
            get
            {
                Portal closestPortal = this.Map.Portals[0];
                double shortestDistance = double.PositiveInfinity;

                foreach (Portal loopPortal in (this.Map.Portals))
                {
                    if (loopPortal.IsSpawnPoint)
                    {
                        double distance = loopPortal.Position.DistanceFrom(this.Position);

                        if (distance < shortestDistance)
                        {
                            closestPortal = loopPortal;
                            shortestDistance = distance;
                        }
                    }
                }

                return closestPortal;
            }
        }

        public Npc LastNpc
        {
            get
            {
                return lastNpc;
            }
            set
            {
                if (lastNpc != null)
                {
                    if (lastNpc.Callbacks.ContainsKey(this))
                    {
                        lastNpc.Callbacks.Remove(this);
                    }

                    if (lastNpc.TextCallbacks.ContainsKey(this))
                    {
                        lastNpc.TextCallbacks.Remove(this);
                    }

                    if (lastNpc.StyleSelectionHelpers.ContainsKey(this))
                    {
                        lastNpc.StyleSelectionHelpers.Remove(this);
                    }
                }

                lastNpc = value;
            }
        }

        public override int ObjectID
        {
            get
            {
                return (int)this.ID;
            }
            set
            {
                return;
            }
        }

        public byte Channel
        {
            get
            {
                return ChannelServer.ChannelID;
            }
        }

        private bool Assigned { get; set; }

        public Character(int id = 0, ChannelClientHandler client = null)
        {
            this.ID = id;
            this.Client = client;

            this.Items = new CharacterItems(this, 24, 48, 24, 24); // TODO: Find the actual max slots defaults.
            this.Skills = new CharacterSkills(this);
            this.Quests = new CharacterQuests(this);
            this.Buffs = new CharacterBuffs(this);
            this.KeyMap = new CharacterKeyMap(this);
            this.BuddyList = new CharacterBuddyList(this);

            this.Position = new Point(0, 0);
            this.ControlledMobs = new ControlledMobs(this);
            this.ControlledNpcs = new ControlledNpcs(this);
        }

        public void Load()
        {
            dynamic datum = new Datum("characters");

            datum.Populate("ID = '{0}'", this.ID);

            this.ID = datum.ID;
            this.Assigned = true;

            this.AccountID = datum.AccountID;
            this.Name = datum.Name;
            this.Level = datum.Level;
            this.Experience = datum.Experience;
            this.Job = (Job)datum.Job;
            this.Strength = datum.Strength;
            this.Dexterity = datum.Dexterity;
            this.Luck = datum.Luck;
            this.Intelligence = datum.Intelligence;
            this.MaxHP = datum.MaxHP;
            this.MaxMP = datum.MaxMP;
            this.CurrentHP = datum.CurrentHP;
            this.CurrentMP = datum.CurrentMP;
            this.Meso = datum.Meso;
            this.Fame = datum.Fame;
            this.Gender = (Gender)datum.Gender;
            this.Hair = datum.Hair;
            this.Skin = datum.Skin;
            this.Face = datum.Face;
            this.AvailableAP = datum.AvailableAP;
            this.AvailableSP = datum.AvailableSP;
            this.SpawnPoint = datum.SpawnPoint;
            this.MaxBuddies = datum.MaxBuddies;
            this.Map = World.Maps[datum.MapID];

            this.Items.MaxSlots[ItemType.Equipment] = datum.EquipmentSlots;
            this.Items.MaxSlots[ItemType.Usable] = datum.UsableSlots;
            this.Items.MaxSlots[ItemType.Setup] = datum.SetupSlots;
            this.Items.MaxSlots[ItemType.Etcetera] = datum.EtceteraSlots;

            this.Items.Load();
            this.Skills.Load();
            this.Buffs.Load();
            this.Quests.Load();
            this.KeyMap.Load();
            this.BuddyList.Load();
        }

        public void Save()
        {
            if (this.IsInitialized)
            {
                this.SpawnPoint = this.ClosestSpawnPoint.ID;
            }

            dynamic datum = new Datum("characters");

            datum.AccountID = this.AccountID;
            datum.Name = this.Name;
            datum.Level = this.Level;
            datum.Experience = this.Experience;
            datum.Job = (int)this.Job;
            datum.Strength = this.Strength;
            datum.Dexterity = this.Dexterity;
            datum.Luck = this.Luck;
            datum.Intelligence = this.Intelligence;
            datum.MaxHP = this.MaxHP;
            datum.MaxMP = this.MaxMP;
            datum.CurrentHP = this.CurrentHP;
            datum.CurrentMP = this.CurrentMP;
            datum.Meso = this.Meso;
            datum.Fame = this.Fame;
            datum.Gender = (byte)this.Gender;
            datum.Hair = this.Hair;
            datum.Skin = this.Skin;
            datum.Face = this.Face;
            datum.AvailableAP = this.AvailableAP;
            datum.AvailableSP = this.AvailableSP;
            datum.SpawnPoint = this.SpawnPoint;
            datum.MaxBuddies = this.MaxBuddies;
            datum.MapID = this.Map.MapleID;

            datum.EquipmentSlots = this.Items.MaxSlots[ItemType.Equipment];
            datum.UsableSlots = this.Items.MaxSlots[ItemType.Usable];
            datum.SetupSlots = this.Items.MaxSlots[ItemType.Setup];
            datum.EtceteraSlots = this.Items.MaxSlots[ItemType.Etcetera];

            if (this.Assigned)
            {
                datum.Update("ID = '{0}'", this.ID);
            }
            else
            {
                datum.Insert();

                this.ID = Database.Fetch("characters", "ID", "Name = '{0}'", this.Name);

                this.Assigned = true;
            }

            this.Items.Save();
            this.Skills.Save();
            this.Buffs.Save();
            this.Quests.Save();
            this.KeyMap.Save();
            this.BuddyList.Save();

            Log.Inform("Saved character '{0}' to database.", this.Name);
        }

        public void Delete()
        {
            this.Items.Delete();
            this.Skills.Delete();
            this.Buffs.Delete();
            this.Quests.Delete();
            this.KeyMap.Delete();
            this.BuddyList.Delete();

            Database.Delete("characters", "ID = '{0}'", this.ID);

            this.Assigned = false;
        }

        public byte[] ToByteArray(bool fromViewAll)
        {
            using (ByteBuffer buffer = new ByteBuffer())
            {
                buffer.WriteBytes(this.StatisticsToByteArray());
                buffer.WriteBytes(this.AppearanceToByteArray(false));
                if (!fromViewAll)
                    buffer.WriteByte();
                if (this.IsMaster)
                    buffer.WriteByte();

                buffer.WriteBool(true); // World rank enabled (next 4 ints are not sent if disabled)

                //TODO: Ranking.
                buffer.WriteInt();
                buffer.WriteInt();
                buffer.WriteInt();
                buffer.WriteInt();

                buffer.Flip();
                return buffer.GetContent();
            }
        }

        public byte[] StatisticsToByteArray()
        {
            using (ByteBuffer buffer = new ByteBuffer())
            {
                buffer.WriteInt(this.ID);
                buffer.WriteStringFixed(this.Name, 13);
                buffer.WriteByte((byte)this.Gender);
                buffer.WriteByte(this.Skin);
                buffer.WriteInt(this.Face);
                buffer.WriteInt(this.Hair);
                buffer.WriteLong(); // UNK: One of these creation date?
                buffer.WriteLong();
                buffer.WriteLong();
                buffer.WriteByte(this.Level);
                buffer.WriteShort((short)this.Job);
                buffer.WriteShort(this.Strength);
                buffer.WriteShort(this.Dexterity);
                buffer.WriteShort(this.Intelligence);
                buffer.WriteShort(this.Luck);
                buffer.WriteShort(this.CurrentHP);
                buffer.WriteShort(this.MaxHP);
                buffer.WriteShort(this.CurrentMP);
                buffer.WriteShort(this.MaxMP);
                buffer.WriteShort(this.AvailableAP);
                buffer.WriteShort(this.AvailableSP);
                buffer.WriteInt(this.Experience);
                buffer.WriteShort(this.Fame);
                buffer.WriteInt();
                buffer.WriteInt(this.Map.MapleID);
                buffer.WriteByte(this.SpawnPoint);
                buffer.WriteInt();

                buffer.Flip();
                return buffer.GetContent();
            }
        }

        public byte[] AppearanceToByteArray(bool forMegaphone = false)
        {
            using (ByteBuffer buffer = new ByteBuffer())
            {
                buffer.WriteByte((byte)this.Gender);
                buffer.WriteByte(this.Skin);
                buffer.WriteInt(this.Face);
                buffer.WriteBool(forMegaphone);
                buffer.WriteInt(this.Hair);

                Dictionary<byte, int> visibleLayer = new Dictionary<byte, int>();
                Dictionary<byte, int> hiddenLayer = new Dictionary<byte, int>();

                foreach (Item item in this.Items.GetEquipped())
                {
                    byte position = item.AbsoluteSlot;

                    if (position < 100 && !visibleLayer.ContainsKey(position))
                    {
                        visibleLayer[position] = item.MapleID;
                    }
                    else if (position > 100 && position != 111)
                    {
                        position -= 100;

                        if (visibleLayer.ContainsKey(position))
                        {
                            hiddenLayer[position] = visibleLayer[position];
                        }

                        visibleLayer[position] = item.MapleID;
                    }
                    else if (visibleLayer.ContainsKey(position))
                    {
                        hiddenLayer[position] = item.MapleID;
                    }
                }

                foreach (KeyValuePair<byte, int> entry in visibleLayer)
                {
                    buffer.WriteByte(entry.Key);
                    buffer.WriteInt(entry.Value);
                }

                buffer.WriteByte(0xFF);

                foreach (KeyValuePair<byte, int> entry in hiddenLayer)
                {
                    buffer.WriteByte(entry.Key);
                    buffer.WriteInt(entry.Value);
                }

                buffer.WriteByte(0xFF);

                Item cashWeapon = this.Items[EquipmentSlot.CashWeapon];

                if (cashWeapon != null)
                {
                    buffer.WriteInt(cashWeapon.MapleID);
                }
                else
                {
                    buffer.WriteInt();
                }

                buffer.Skip(12);

                buffer.Flip();
                return buffer.GetContent();
            }
        }

        public byte[] InformationToByteArray()
        {
            using (ByteBuffer writer = new ByteBuffer())
            {
                writer.WriteInt(this.ID);
                writer.WriteByte(this.Level);
                writer.WriteShort(this.IsMaster ? (short)Job.GM : (short)this.Job);
                writer.WriteShort(this.Fame);
                writer.WriteBool(false); // TODO: Married.
                writer.WriteString("-"); // TODO: Guild name.
                writer.WriteString(string.Empty); // TODO: Alliance name.
                writer.WriteByte();
                writer.WriteShort(0); // Pets footer.
                writer.WriteByte(0); // TODO: Wishlist (this is the number of items in it.)
                writer.WriteInt(1);
                writer.WriteLong();
                writer.WriteLong();
                writer.Flip();

                return writer.GetContent();
            }
        }

        public void Initialize()
        {
            using (Packet outPacket = new Packet(MapleServerOperationCode.WarpToMap))
            {
                outPacket.WriteInt(ChannelServer.InternalChannelID);
                outPacket.WriteByte(1);
                outPacket.WriteByte(1);
                outPacket.WriteShort();

                for (int i = 0; i < 3; i++)
                {
                    outPacket.WriteInt(Application.Random.Next());
                }

                outPacket.WriteBytes(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF });
                outPacket.WriteByte();
                outPacket.WriteBytes(this.StatisticsToByteArray());
                outPacket.WriteByte(this.MaxBuddies);
                outPacket.WriteByte();
                outPacket.WriteInt(this.Meso);
                outPacket.WriteBytes(this.Items.ToByteArray());
                outPacket.WriteBytes(this.Skills.ToByteArray());
                outPacket.WriteBytes(this.Quests.ToByteArray());

                // TODO: Rings.
                outPacket.WriteShort();
                outPacket.WriteShort();
                outPacket.WriteShort();
                outPacket.WriteShort(); // Somehow ring footer.

                for (int i = 0; i < 15; i++)
                {
                    outPacket.WriteBytes(PacketConstants.Character);
                }

                // TODO: Monster book.
                outPacket.WriteInt(); // Cover.
                outPacket.WriteByte();
                outPacket.WriteShort(0); // Cards length.

                outPacket.WriteShort(); // UNK: PQ Information?
                outPacket.WriteShort();
                outPacket.WriteShort();

                outPacket.WriteDateTime(DateTime.UtcNow);

                this.Client.Send(outPacket);
            }

            this.Map.Characters.Add(this);

            this.IsInitialized = true;

            this.KeyMap.Send();
            this.BuddyList.Update();

            // NOTE: Until we find out more about buffs in the SpawnPlayer packet

            foreach (Buff loopBuff in this.Buffs)
            {
                loopBuff.Apply();
            }

            this.UpdateBuddies(true);
        }

        public Packet GetCreatePacket()
        {
            return this.GetSpawnPacket();
        }

        public Packet GetSpawnPacket()
        {
            Packet spawn = new Packet(MapleServerOperationCode.SpawnCharacter);

            spawn.WriteInt(this.ID);
            spawn.WriteByte(this.Level);
            spawn.WriteString(this.Name);

            // If not in guild:
            spawn.WriteString(string.Empty);
            spawn.Skip(6);

            spawn.WriteInt(); // UNK: Maybe not an int.
            spawn.WriteShort();
            spawn.WriteByte(0xFC);
            spawn.WriteByte(1);
            spawn.WriteInt(0x00); // TODO: 2 if morphed.

            // TODO: Buffs?
            long buffMask = 0;
            spawn.WriteInt((int)((buffMask >> 32) & 0xffffffffL));
            spawn.WriteInt((int)(buffMask & 0xffffffffL));

            int characterSpawn = Application.Random.Next();
            spawn.Skip(6);
            spawn.WriteInt(characterSpawn);
            spawn.Skip(11);
            spawn.WriteInt(characterSpawn);
            spawn.Skip(11);
            spawn.WriteInt(characterSpawn);
            spawn.Skip(3);

            // TODO: Mount.
            spawn.WriteLong();

            spawn.WriteInt(characterSpawn);
            spawn.Skip(9);
            spawn.WriteInt(characterSpawn);
            spawn.Skip(2);
            spawn.WriteInt(); // UNK.
            spawn.Skip(10);
            spawn.WriteInt(characterSpawn);
            spawn.Skip(13);
            spawn.WriteInt(characterSpawn);
            spawn.Skip(3);

            spawn.WriteShort((short)this.Job);
            spawn.WriteBytes(this.AppearanceToByteArray(false));
            spawn.WriteInt();
            spawn.WriteInt(); // TODO: Item effect.
            spawn.WriteInt(); // TODO: Chair.
            spawn.WriteShort(this.Position.X);
            spawn.WriteShort(this.Position.Y);
            spawn.WriteByte(this.Stance);
            spawn.WriteShort();
            spawn.WriteByte();

            // TODO: Pets.
            spawn.WriteByte();

            spawn.WriteInt(1); // TODO: Mount level.
            spawn.WriteInt(); // TODO: Mount experience.
            spawn.WriteInt(); // TODO: Mount tiredness.

            // TODO: Player interaction.
            spawn.WriteByte();

            // TODO: Chalkboard.
            spawn.WriteByte();

            // TODO: Rings.
            spawn.WriteByte();
            spawn.WriteByte();

            // TODO: Marriage rings.
            spawn.WriteByte();

            spawn.Skip(3);

            spawn.WriteByte();

            return spawn;
        }

        public Packet GetDestroyPacket()
        {
            Packet destroy = new Packet(MapleServerOperationCode.RemoveCharacter);

            destroy.WriteInt(this.ID);

            return destroy;
        }

        public void Move(ByteBuffer reader)
        {
            reader.Skip(9);

            Movements movements = Movements.Parse(reader.ReadBytes());

            foreach (Movement movement in movements)
            {
                if (movement is AbsoluteMovement)
                {
                    this.Position = ((AbsoluteMovement)movement).Position;
                }

                if (!(movement is EquipmentMovement))
                {
                    this.Stance = movement.NewStance;
                }
            }

            using (Packet outPacket = new Packet(MapleServerOperationCode.MoveCharacter))
            {
                outPacket.WriteInt(this.ID);
                outPacket.WriteInt();
                outPacket.WriteBytes(movements.ToByteArray());

                this.Map.Broadcast(this, outPacket);
            }
        }

        public void Express(int expressionID)
        {
            using (Packet outPacket = new Packet(MapleServerOperationCode.FacialExpression))
            {
                outPacket.WriteInt(this.ID);
                outPacket.WriteInt(expressionID);

                this.Map.Broadcast(this, outPacket);
            }
        }

        public void Express(ByteBuffer reader)
        {
            this.Express(reader.ReadInt());
        }

        public void Talk(string text)
        {
            text = text.Replace("{", "{{").Replace("}", "}}");

            if (text.StartsWith(Application.CommandIndicator) || text.StartsWith(Application.PlayerCommandIndicator))
            {
                CommandFactory.Execute(this, text);
            }
            else
            {
                using (Packet outPacket = new Packet(MapleServerOperationCode.ChatText))
                {
                    outPacket.WriteInt(this.ID);
                    outPacket.WriteBool(this.IsMaster);
                    outPacket.WriteString(text);
                    outPacket.WriteBool(false); // Hide from log.
                    outPacket.Flip();

                    this.Map.Broadcast(outPacket);
                }
            }
        }

        public void Talk(ByteBuffer reader)
        {
            this.Talk(reader.ReadString());
        }

        public void Notify(string message, NoticeType type = NoticeType.Pink)
        {
            using (Packet outPacket = new Packet(MapleServerOperationCode.ServerMessage))
            {
                outPacket.WriteByte((byte)type);
                outPacket.WriteString(message);
                this.Client.Send(outPacket);
            }
        }

        public void ChangeMap(int destinationMapId, byte portalId = 0)
        {
            using (Packet outPacket = new Packet(MapleServerOperationCode.WarpToMap))
            {
                outPacket.WriteInt(ChannelServer.InternalChannelID);
                outPacket.WriteInt();
                outPacket.WriteByte();
                outPacket.WriteInt(destinationMapId);
                outPacket.WriteByte(portalId);
                outPacket.WriteShort(this.CurrentHP);
                outPacket.WriteByte();
                outPacket.WriteLong(0x1FFFFFFFFFFFFFFL); // Quest mask?

                this.Client.Send(outPacket);
            }

            // TODO: Jump stance, spawn higher from portal
            // this.Stance = 0; 

            this.SpawnPoint = portalId;
            this.Map.Characters.Remove(this);
            World.Maps[destinationMapId].Characters.Add(this);
        }

        public void ChangeMap(Packet inPacket)
        {
            ChangeMapMode mode = (ChangeMapMode)inPacket.ReadByte();
            int destinationMapId = inPacket.ReadInt();
            string portalLabel = inPacket.ReadString();
            inPacket.ReadByte();
            bool wheel = inPacket.ReadShort() > 0;

            if (!this.IsAlive && this.IsMaster)
            {
                this.CurrentHP = this.MaxHP;
            }
            else if (destinationMapId != -1)
            {
                this.ChangeMap(destinationMapId);

                if (!this.IsAlive)
                {
                    this.CurrentHP = 50;
                }
            }
            else if (portalLabel != null)
            {
                this.ChangeMap(this.Map.Portals[portalLabel].DestinationMapID, this.Map.Portals[portalLabel].Link.ID);
            }
            else
            {
                this.Release();
            }
        }

        public void ChangeMapSpecial(Packet inPacket)
        {
            inPacket.ReadByte();
            String portalLabel = inPacket.ReadString();

            Portal portal = this.Map.Portals[portalLabel];

            this.Release();
        }

        public void Release()
        {
            this.UpdateStatistics();
        }

        public void InformOnCharacter(Packet inPacket)
        {
            inPacket.ReadShort();
            inPacket.ReadShort();
            int characterID = inPacket.ReadInt();

            this.InformOnCharacter(this.Map.Characters[characterID]);
        }

        public void InformOnCharacter(Character character)
        {
            using (Packet outPacket = new Packet(MapleServerOperationCode.CharacterInformation))
            {
                outPacket.WriteBytes(character.InformationToByteArray());

                this.Client.Send(outPacket);
            }
        }

        public void UpdateLook()
        {
            using (Packet outPacket = new Packet(MapleServerOperationCode.UpdateCharacterLook))
            {
                outPacket.WriteInt(this.ID);
                outPacket.WriteByte(1); // UNK.
                outPacket.WriteBytes(this.AppearanceToByteArray());
                outPacket.WriteByte(0); // TODO: Ring footer.
                outPacket.WriteShort();

                this.Map.Broadcast(outPacket);
            }
        }

        public void UpdateStatistics(params StatisticType[] statistics)
        {
            using (Packet outPacket = new Packet(MapleServerOperationCode.UpdateStats))
            {
                outPacket.WriteBool(true); // TODO: Item reaction?

                int mask = 0;

                foreach (StatisticType loopStatistic in statistics)
                {
                    mask |= (int)loopStatistic;
                }

                outPacket.WriteInt(mask);

                Array.Sort(statistics);

                foreach (StatisticType loopStatistic in statistics) // NOTE: sorted statistics here
                {
                    switch (loopStatistic)
                    {
                        case StatisticType.Skin:
                            outPacket.WriteShort(this.Skin);
                            break;

                        case StatisticType.Face:
                            outPacket.WriteInt(this.Face);
                            break;

                        case StatisticType.Hair:
                            outPacket.WriteInt(this.Hair);
                            break;

                        case StatisticType.Level:
                            outPacket.WriteByte(this.Level);
                            break;

                        case StatisticType.Job:
                            outPacket.WriteShort((short)this.Job);
                            break;

                        case StatisticType.Strength:
                            outPacket.WriteInt(this.Strength);
                            break;

                        case StatisticType.Dexterity:
                            outPacket.WriteInt(this.Dexterity);
                            break;

                        case StatisticType.Intelligence:
                            outPacket.WriteInt(this.Intelligence);
                            break;

                        case StatisticType.Luck:
                            outPacket.WriteInt(this.Luck);
                            break;

                        case StatisticType.CurrentHP:
                            outPacket.WriteInt(this.CurrentHP);
                            break;

                        case StatisticType.MaxHP:
                            outPacket.WriteInt(this.MaxHP);
                            break;

                        case StatisticType.CurrentMP:
                            outPacket.WriteInt(this.CurrentMP);
                            break;

                        case StatisticType.MaxMP:
                            outPacket.WriteInt(this.MaxMP);
                            break;

                        case StatisticType.AvailableAP:
                            outPacket.WriteInt(this.AvailableAP);
                            break;

                        case StatisticType.AvailableSP:
                            outPacket.WriteInt(this.AvailableSP);
                            break;

                        case StatisticType.Experience:
                            outPacket.WriteInt(this.Experience);
                            break;

                        case StatisticType.Fame:
                            outPacket.WriteInt(this.Fame);
                            break;

                        case StatisticType.Meso:
                            outPacket.WriteInt(this.Meso);
                            break;
                    }
                }

                this.Client.Send(outPacket);
            }
        }

        public void ShowEffect(ForeignEffect effect)
        {
            using (Packet outPacket = new Packet(MapleServerOperationCode.ShowForeignEffect))
            {
                outPacket.WriteInt(this.ID);
                outPacket.WriteByte((byte)effect);

                this.Map.Broadcast(outPacket);
            }
        }

        public void Converse(Npc npc)
        {
            if (!this.IsAlive)
            {
                this.Release();
            }

            this.LastNpc = npc;
            this.LastNpc.Converse(this, new NpcEventArgs(0, -1));
        }

        public void Converse(int npcMapleId)
        {
            this.Converse(World.Npcs[npcMapleId]);
        }

        public void Converse(Packet inPacket)
        {
            this.Converse(this.Map.Npcs[inPacket.ReadInt()]);
        }

        public void DistributeAP(Packet inPacket)
        {
            lock (this)
            {
                if (this.AvailableAP < 1)
                {
                    return;
                }
                else
                {
                    inPacket.ReadInt();

                    switch (inPacket.ReadInt())
                    {
                        case 64:
                            if (this.Strength == 32767)
                                return;
                            this.Strength++;
                            break;

                        case 128:
                            if (this.Dexterity == 32767)
                                return;
                            this.Dexterity++;
                            break;

                        case 256:
                            if (this.Intelligence == 32767)
                                return;
                            this.Intelligence++;
                            break;

                        case 512:
                            if (this.Luck == 32767)
                                return;
                            this.Luck++;
                            break;

                        case 2048:
                            if (this.MaxHP == 30000)
                                return;
                            this.MaxHP += 10; // TODO: Correct HP addition.
                            break;

                        case 8192:
                            if (this.MaxMP == 30000)
                                return;
                            this.MaxMP += 10; // TODO: Correct MP addition.
                            break;

                        default:
                            throw new NotImplementedException("Adding unknown statistic.");
                    }

                    this.AvailableAP--;

                    this.Release();
                }
            }
        }

        public void DistributeAP(int stat)
        {
            lock (this)
            {
                switch (stat)
                {
                    case 64:
                        if (this.Strength == 32767)
                            return;
                        this.Strength++;
                        break;

                    case 128:
                        if (this.Dexterity == 32767)
                            return;
                        this.Dexterity++;
                        this.Dexterity++;
                        break;

                    case 256:
                        if (this.Intelligence == 32767)
                            return;
                        this.Intelligence++;
                        break;

                    case 512:
                        if (this.Luck == 32767)
                            return;
                        this.Luck++;
                        break;

                    case 2048:
                        if (this.MaxHP == 30000)
                            return;
                        this.MaxHP += 10; // TODO: Correct HP addition.
                        break;

                    case 8192:
                        if (this.MaxMP == 30000)
                            return;
                        this.MaxMP += 10; // TODO: Correct MP addition.
                        break;

                    default:
                        throw new NotImplementedException("Adding unknown statistic.");
                }

                this.AvailableAP--;

                this.Release();
            }
        }

        public void DistributeSP(Packet inPacket)
        {
            if (this.AvailableSP < 1)
            {
                return;
            }
            else
            {
                inPacket.ReadInt();

                int skillID = inPacket.ReadInt();

                if (!this.Skills.Contains(skillID))
                {
                    this.Skills.Add(new Skill(skillID));
                }

                Skill skill = this.Skills[skillID];

                // TODO: Check for skill requirements.

                if (skill.IsFromBeginner)
                {
                    //TODO: Handle begginner skills
                }

                if (skill.CurrentLevel + 1 <= skill.MaxLevel)
                {
                    if (!skill.IsFromBeginner)
                    {
                        this.AvailableSP--;
                    }

                    this.Release();

                    skill.CurrentLevel++;
                }
                else
                {
                    return;
                }
            }
        }

        public void HealOverTime(Packet inPacket)
        {
            inPacket.ReadInt();
            short hpAmount = inPacket.ReadShort();
            short mpAmount = inPacket.ReadShort();

            if (hpAmount != 0)
            {
                if ((DateTime.Now - this.LastHealHPOverTime).Seconds < 2)
                {
                    return;
                }
                else
                {
                    this.CurrentHP += hpAmount; // Check for hacks with skill levels
                    this.LastHealHPOverTime = DateTime.Now;
                }
            }

            if (mpAmount != 0)
            {
                if ((DateTime.Now - this.LastHealMPOverTime).Seconds < 2)
                {
                    return;
                }
                else
                {
                    this.CurrentMP += mpAmount; // Check for hacks with skill levels
                    this.LastHealMPOverTime = DateTime.Now;
                }
            }
        }

        public void DropMeso(Packet inPacket)
        {
            if (!this.IsAlive) // TODO: Is this needed?
            {
                this.Release();
            }
            else
            {
                inPacket.ReadInt();
                int amount = inPacket.ReadInt();

                if (amount > this.Meso || amount < 10 || amount > 50000)
                {
                    throw new HackException("Illegal meso drop.");
                }
                else
                {
                    this.Meso -= amount;

                    Meso meso = new Meso(amount)
                    {
                        Dropper = this,
                        Owner = null
                    };

                    this.Map.Drops.Add(meso);
                }
            }
        }

        public void Attack(Packet inPacket, AttackType type)
        {
            if (!this.IsAlive) // Needed?
            {
                return;
            }

            inPacket.ReadByte();
            byte amountAttackedDamaged = inPacket.ReadByte();
            byte amountAttacked = (byte)((amountAttackedDamaged >> 4) & 0xF);
            byte amountDamaged = (byte)(amountAttackedDamaged & 0xF);
            int skillID = inPacket.ReadInt();

            Skill skill = null;

            if (skillID > 0)
            {
                skill = this.Skills[skillID];

                if (!skill.Cast())
                {
                    return;
                }
            }

            int charge;

            if (skillID == 2121001 || skillID == 2221001 || skillID == 2321001 || skillID == 5201002 || skillID == 5101004)
            {
                charge = inPacket.ReadInt();
            }
            else
            {
                charge = 0;
            }

            inPacket.Skip(10);
            byte stance = inPacket.ReadByte();

            if (skillID == 4211006)
            {
                // TODO: Meso Explosion.
            }

            inPacket.ReadByte();
            byte speed = inPacket.ReadByte();

            if (type == AttackType.Ranged)
            {
                inPacket.ReadByte();
                byte direction = inPacket.ReadByte();
                inPacket.Skip(7);

                if (skillID == 3121004 || skillID == 3221001 || skillID == 5221004)
                {
                    inPacket.Skip(4);
                }
            }
            else
            {
                inPacket.Skip(4);
            }

            List<Mob> dead = new List<Mob>();

            if (type == AttackType.CloseRange)
            {
                using (Packet outPacket = new Packet(MapleServerOperationCode.CloseRangeAttack))
                {
                    if (skillID == 4211006)
                    {
                        // TODO: Meso Explosion
                    }
                    else
                    {
                        outPacket.WriteInt(this.ID);
                        outPacket.WriteByte(amountAttackedDamaged);
                        outPacket.WriteByte(0x5B);

                        if (this.Skills.Contains(skillID))
                        {
                            outPacket.WriteByte(this.Skills[skillID].CurrentLevel);

                            if (this.Skills[skillID].CurrentLevel > 0)
                                outPacket.WriteInt(skillID);
                        }
                        else
                        {
                            outPacket.WriteByte();
                        }

                        outPacket.WriteByte();
                        outPacket.WriteByte();
                        outPacket.WriteByte(stance);
                        outPacket.WriteByte(speed);
                        outPacket.WriteByte(0x0A);
                        outPacket.WriteInt();

                        for (int i = 0; i < amountAttacked; i++)
                        {
                            int objectId = inPacket.ReadInt();
                            inPacket.Skip(14);

                            lock (this.Map.Mobs)
                            {
                                if (this.Map.Mobs.Contains(objectId))
                                {
                                    Mob target = this.Map.Mobs[objectId];
                                    target.IsProvoked = true;
                                    target.SwitchController(this);

                                    outPacket.WriteInt(target.ObjectID);
                                    outPacket.WriteByte(0xFF);

                                    uint totalDamage = 0;

                                    for (int j = 0; j < amountDamaged; j++)
                                    {
                                        uint value = inPacket.ReadUInt();
                                        totalDamage += value;
                                        outPacket.WriteUInt(value);
                                    }


                                    if (target.Damage(this, totalDamage))
                                    {
                                        //this.HandleComboOrbGain();
                                        //this.HandleEnergyChargeGain();

                                        dead.Add(target);
                                    }
                                }
                            }

                            if (skillID != 5221004) inPacket.Skip(4);
                        }
                    }

                    this.Map.Broadcast(this, outPacket);
                }
            }
            else if (type == AttackType.Ranged)
            {
                int projectile = this.Items.GetProjectile(skill);

                if (projectile == 0 && skillID != 5121002) // NOTE: Energy orb doesn't use a projectile
                {
                    return;
                    // Player is using skill without projectiles
                }

                using (Packet outPacket = new Packet(MapleServerOperationCode.RangedAttack))
                {
                    outPacket.WriteInt(this.ID);
                    outPacket.WriteByte(amountAttackedDamaged);
                    outPacket.WriteByte(0x5B);

                    if (this.Skills.Contains(skillID))
                    {
                        outPacket.WriteByte(this.Skills[skillID].CurrentLevel);

                        if (this.Skills[skillID].CurrentLevel > 0)
                            outPacket.WriteInt(skillID);
                    }
                    else
                    {
                        outPacket.WriteByte();
                    }

                    outPacket.WriteByte();
                    outPacket.WriteByte();
                    outPacket.WriteByte(stance);
                    outPacket.WriteByte(speed);
                    outPacket.WriteByte(0x0A);
                    outPacket.WriteInt(projectile);

                    for (int i = 0; i < amountAttacked; i++)
                    {
                        int objectId = inPacket.ReadInt();
                        inPacket.Skip(14);

                        lock (this.Map.Mobs)
                        {
                            if (this.Map.Mobs.Contains(objectId))
                            {
                                Mob target = this.Map.Mobs[objectId];
                                target.IsProvoked = true;
                                target.SwitchController(this);

                                outPacket.WriteInt(target.ObjectID);
                                outPacket.WriteByte(0xFF);

                                uint totalDamage = 0;

                                for (int j = 0; j < amountDamaged; j++)
                                {
                                    uint value = inPacket.ReadUInt();
                                    totalDamage += value;
                                    outPacket.WriteUInt(value);
                                }

                                if (target.Damage(this, totalDamage))
                                {
                                    dead.Add(target);
                                }
                            }
                        }

                        if (skillID != 5221004) inPacket.Skip(4);
                    }
                    outPacket.WriteInt();
                    this.Map.Broadcast(this, outPacket);
                }
            }
            else if (type == AttackType.Magic)
            {
                using (Packet outPacket = new Packet(MapleServerOperationCode.CloseRangeAttack))
                {
                    outPacket.WriteInt(this.ID);
                    outPacket.WriteByte(amountAttackedDamaged);
                    outPacket.WriteByte(0x5B);

                    if (this.Skills.Contains(skillID))
                    {
                        outPacket.WriteByte(this.Skills[skillID].CurrentLevel);

                        if (this.Skills[skillID].CurrentLevel > 0)
                            outPacket.WriteInt(skillID);
                    }
                    else
                    {
                        outPacket.WriteByte();
                    }

                    outPacket.WriteByte();
                    outPacket.WriteByte();
                    outPacket.WriteByte(stance);
                    outPacket.WriteByte(speed);
                    outPacket.WriteByte(0x0A);
                    outPacket.WriteInt();

                    for (int i = 0; i < amountAttacked; i++)
                    {
                        int objectId = inPacket.ReadInt();
                        inPacket.Skip(14);

                        lock (this.Map.Mobs)
                        {
                            if (this.Map.Mobs.Contains(objectId))
                            {
                                Mob target = this.Map.Mobs[objectId];
                                target.IsProvoked = true;
                                target.SwitchController(this);

                                outPacket.WriteInt(target.ObjectID);
                                outPacket.WriteByte(0xFF);

                                uint totalDamage = 0;

                                for (int j = 0; j < amountDamaged; j++)
                                {
                                    uint value = inPacket.ReadUInt();
                                    totalDamage += value;
                                    outPacket.WriteUInt(value);
                                }


                                if (target.Damage(this, totalDamage))
                                {
                                    dead.Add(target);
                                }
                            }
                        }

                        if (skillID != 5221004) inPacket.Skip(4);
                    }

                    if (skillID == 2121001 || skillID == 2221001 || skillID == 2321001)
                    {
                        outPacket.WriteInt(charge);
                    }

                    this.Map.Broadcast(this, outPacket);
                }
            }

            foreach (Mob deadMob in dead)
            {
                deadMob.Die();
            }
        }

        public void UseSpecialMove(Packet inPacket)
        {
            inPacket.ReadInt();
            Skill skill = this.Skills[inPacket.ReadInt()];
            int level = inPacket.ReadByte();

            if (level != skill.CurrentLevel)
            {
                throw new HackException("Casting invalid skill level.");
            }
            else
            {
                skill.Recalculate();
                skill.Cast();
            }
        }

        public void Interact(Packet inPacket)
        {
            InteractionCode action = (InteractionCode)inPacket.ReadByte();

            switch (action)
            {
                case InteractionCode.Create:

                    switch ((InteractionType)inPacket.ReadByte())
                    {
                        case InteractionType.Trade:

                            if (this.Trade == null)
                            {
                                this.Trade = new Trade(this);
                            }

                            break;

                        case InteractionType.PlayerShop:

                            if (this.PlayerShop == null)
                            {
                                string description = inPacket.ReadString();

                                this.PlayerShop = new PlayerShop(this, description);
                            }

                            break;

                        case InteractionType.HiredMerchant:

                            if (this.HiredMerchant == null)
                            {
                                string description = inPacket.ReadString();
                                inPacket.Skip(3);
                                int itemId = inPacket.ReadInt();

                                this.HiredMerchant = new HiredMerchant(this, description, itemId);
                            }

                            break;

                        case InteractionType.Omok:

                            if (this.Omok == null)
                            {
                                string description = inPacket.ReadString();
                                inPacket.ReadByte();
                                int gameType = inPacket.ReadByte();

                                this.Omok = new Omok(this, description, gameType);
                            }

                            break;

                        default:
                            Log.Warn("Unrecognized interaction type.");
                            break;
                    }

                    break;

                case InteractionCode.Visit:

                    if (this.Trade == null && this.Omok == null && this.PlayerShop == null && this.HiredMerchant == null)
                    {
                        int objectId = inPacket.ReadInt();

                        if (this.Map.PlayerShops.Contains(objectId))
                        {
                            this.Map.PlayerShops[objectId].AddVisitor(this);
                        }
                        else if (this.Map.HiredMerchants.Contains(objectId))
                        {
                            this.Map.HiredMerchants[objectId].AddVisitor(this);
                        }
                        else if (this.Map.Omoks.Contains(objectId))
                        {
                            this.Map.Omoks[objectId].AddVisitor(this);
                        }
                        else
                        {
                            Log.Warn("No proper interaction found to visit.");
                        }
                    }

                    break;

                default:

                    if (this.Trade != null)
                    {
                        this.Trade.Handle(this, action, inPacket);
                    }
                    else if (this.PlayerShop != null)
                    {
                        this.PlayerShop.Handle(this, action, inPacket);
                    }
                    else if (this.Omok != null)
                    {
                        this.Omok.Handle(this, action, inPacket);
                    }
                    else if (this.HiredMerchant != null)
                    {
                        this.HiredMerchant.Handle(this, action, inPacket);
                    }
                    else
                    {
                        Log.Warn("No proper interaction found to handle.");
                    }

                    break;
            }
        }

        public void Damage(Packet inPacket)
        {
            inPacket.ReadInt();
            int damageFrom = inPacket.ReadByte();
            inPacket.ReadByte();
            int damage = inPacket.ReadInt();
            int objectId = 0;
            int monsterIdFrom = 0;
            int pgmr = 0;
            int direction = 0;
            Point position = new Point(0, 0);
            int fake = 0;
            bool isPgmr = false;
            bool isPg = true;
            int mpAttack = 0;
            Mob attacker = null;

            this.CurrentHP -= (short)damage;
        }

        public void HandleComboOrbGain()
        {
            int comboSkillID = this.IsCygnus ? (int)SkillNames.DawnWarrior.ComboAttack : (int)SkillNames.Crusader.ComboAttack;
            int advancedComboSkillID = this.IsCygnus ? (int)SkillNames.DawnWarrior.AdvancedCombo : (int)SkillNames.Hero.AdvancedComboAttack;

            if (this.Buffs.Contains(comboSkillID))
            {
                Buff buff = this.Buffs[comboSkillID];
                Skill skill = this.Skills[comboSkillID];

                if (this.Skills.Contains(advancedComboSkillID))
                {
                    if (this.Skills[advancedComboSkillID].CurrentLevel > 0)
                    {
                        skill = this.Skills[advancedComboSkillID];
                    }
                }

                if (buff.Value < skill.ParameterA)
                {
                    //TODO: Advanced Combo has a chance of adding 2 orbs
                    buff.SecondaryStatups[SecondaryBuffStat.Combo]++;
                    buff.Value++;

                    //TODO: Find out what happens to the buff duration when orb is gained. Does it reset?

                    buff.Apply();
                }
            }
        }

        public void HandleEnergyChargeGain()
        {
            int energyChargeID = this.IsCygnus ? (int)SkillNames.ThunderBreaker.EnergyCharge : (int)SkillNames.Marauder.EnergyCharge;

            if (this.Skills[energyChargeID] != null)
            {
                if (this.Buffs.Contains(energyChargeID))
                {
                    Buff buff = this.Buffs[energyChargeID];

                    if (buff.Value < 10000)
                    {
                        buff.Value += 102;
                        buff.Value = Math.Min(buff.Value, 10000);

                        if (buff.Value == 10000)
                        {
                            //Start timer
                        }
                        buff.Apply();
                    }
                }
                else
                {
                    Buff energybuff = new Buff(this.Buffs, this.Skills[energyChargeID], 102);
                    energybuff.End = DateTime.Now.AddMinutes(30);
                    //Stop timer
                    this.Buffs.Add(energybuff);
                }
            }
        }

        public void HandleEnergyOrbAttack(Packet inPacket)
        {
            if (!this.IsAlive)
            {
                return;
            }

            inPacket.ReadByte();
            byte amountAttackedDamaged = inPacket.ReadByte();
            byte amountAttacked = (byte)((amountAttackedDamaged >> 4) & 0xF);
            byte amountDamaged = (byte)(amountAttackedDamaged & 0xF);
            int skillID = inPacket.ReadInt();

            Skill skill = this.Skills[skillID];
            inPacket.Skip(9);
            byte stance = inPacket.ReadByte();
            inPacket.ReadByte();
            byte speed = inPacket.ReadByte();

            inPacket.Skip(4);

            using (Packet outPacket = new Packet(MapleServerOperationCode.CloseRangeAttack))
            {
                outPacket.WriteInt(this.ID);
                outPacket.WriteByte(amountAttackedDamaged);
                outPacket.WriteByte(0xFF);
                outPacket.WriteInt(skillID);
                outPacket.WriteByte();
                outPacket.WriteByte(stance);
                outPacket.WriteByte(speed);
                outPacket.WriteByte(0x0A);
                outPacket.WriteInt();

                int objectId = inPacket.ReadInt();
                inPacket.Skip(14);

                lock (this.Map.Mobs)
                {
                    if (this.Map.Mobs.Contains(objectId))
                    {
                        Mob target = this.Map.Mobs[objectId];
                        target.IsProvoked = true;
                        target.SwitchController(this);

                        outPacket.WriteInt(target.ObjectID);
                        outPacket.WriteByte(0xFF);

                        uint totalDamage = 0;

                        uint value = inPacket.ReadUInt();
                        totalDamage += value;
                        outPacket.WriteUInt(value);
                        target.Damage(this, totalDamage);

                    }
                }

                if (skillID != 5221004) inPacket.Skip(4);

                this.Map.Broadcast(this, outPacket);
            }
        }

        public void HandleSkillEffect(Packet inPacket)
        {
            int skillID = inPacket.ReadInt();
            int level = inPacket.ReadByte();
            byte flags = inPacket.ReadByte();
            int speed = inPacket.ReadByte();

            switch (skillID)
            {
                //case (int)SkillNames.FirePoisonMage.Explosion:
                case (int)SkillNames.FirePoisonArchMage.BigBang:
                case (int)SkillNames.IceLightningArchMage.BigBang:
                case (int)SkillNames.Bishop.BigBang:
                case (int)SkillNames.Bowmaster.Hurricane:
                case (int)SkillNames.Marksman.PiercingArrow:
                case (int)SkillNames.ChiefBandit.Chakra:
                case (int)SkillNames.Brawler.CorkscrewBlow:
                case (int)SkillNames.Gunslinger.Grenade:
                case (int)SkillNames.Corsair.RapidFire:
                case (int)SkillNames.WindArcher.Hurricane:
                case (int)SkillNames.NightWalker.PoisonBomb:
                case (int)SkillNames.ThunderBreaker.CorkscrewBlow:

                    using (Packet outPacket = new Packet(MapleServerOperationCode.SkillEffect))
                    {
                        outPacket.WriteInt(this.ID);
                        outPacket.WriteInt(skillID);
                        outPacket.WriteByte((byte)level);
                        outPacket.WriteByte((byte)flags);
                        outPacket.WriteByte((byte)speed);

                        this.Map.Broadcast(outPacket);
                    }

                    break;

                default:

                    Log.Warn("Unrecognized skill effect.");

                    return;

            }
        }

        public void UseHammer(Packet inPacket)
        {
            using (Packet outPacket = new Packet(MapleServerOperationCode.ViciousHammer))
            {
                outPacket.WriteByte(0x38);
                outPacket.WriteInt(0);
                this.Client.Send(outPacket);
            }
        }

        public void UseUpgradeScroll(Packet inPacket)
        {
            inPacket.ReadInt();
            sbyte slot = (sbyte)inPacket.ReadShort();
            sbyte dst = (sbyte)inPacket.ReadShort();
            byte ws = (byte)inPacket.ReadShort();
            bool whiteScroll = false;
            bool legendarySpirit = false;
            if ((ws & 2) == 2)
            {
                whiteScroll = true;
            }
            Item toScroll = null;
            Skill legendarySpiritSkill = new Skill(0001003);
            if (this.Skills.Contains(legendarySpiritSkill))
            {
                if (this.Skills[0001003].CurrentLevel > 0 && dst >= 0)
                {
                    legendarySpirit = true;
                    toScroll = this.Items[ItemType.Equipment, dst];
                }
            }
            else
                toScroll = this.Items[(EquipmentSlot)dst];
            byte oldLevel = toScroll.UpgradesApplied;
            if (toScroll.UpgradesAvailable < 1)
            {
                this.Items.NotifyFull();
                return;
            }
            Item scroll = this.Items[ItemType.Usable, slot];
            if (whiteScroll)
            {
                if (!this.Items.Contains(2340000))
                {
                    whiteScroll = false;
                }
            }
            if (scroll.Flag == "")
            {
                if (!((scroll.MapleID / 100) % 100 == (toScroll.MapleID / 10000) % 100))
                {
                    return;
                }
            }
            if (scroll.Quantity < 1)
            {
                return;
            }
            ScrollResult scrollSuccess = ScrollResult.Success;
            Random r = new Random();
            int rand;
            rand = r.Next(1, 101);
            if (rand <= scroll.Success)
            {
                switch (scroll.Flag)
                {
                    case "":
                        toScroll.UpgradesApplied++;
                        toScroll.UpgradesAvailable--;
                        toScroll.Strength += scroll.IStrength;
                        toScroll.Dexterity += scroll.IDexterity;
                        toScroll.Intelligence += scroll.IIntelligence;
                        toScroll.Luck += scroll.ILuck;
                        toScroll.HP += scroll.IHP;
                        toScroll.MP += scroll.IMP;
                        toScroll.WeaponAttack += scroll.IWeaponAttack;
                        toScroll.MagicAttack += scroll.IMagicAttack;
                        toScroll.WeaponDefense += scroll.IWeaponDefense;
                        toScroll.MagicDefense += scroll.IMagicDefense;
                        toScroll.Accuracy += scroll.IAccuracy;
                        toScroll.Avoidability += scroll.IAvoidability;
                        toScroll.Jump += scroll.IJump;
                        toScroll.Speed += scroll.ISpeed;
                        break;
                    case "rand_stat":
                        int inc = 1;
                        if (r.Next(1, 3) == 1)
                            inc = -1;
                        if (toScroll.Strength > 0)
                            toScroll.Strength = (short)Math.Max(0, toScroll.Strength + r.Next(1, 6) * inc);
                        if (toScroll.Dexterity > 0)
                            toScroll.Dexterity = (short)Math.Max(0, toScroll.Dexterity + r.Next(1, 6) * inc);
                        if (toScroll.Intelligence > 0)
                            toScroll.Intelligence = (short)Math.Max(0, toScroll.Intelligence + r.Next(1, 6) * inc);
                        if (toScroll.Luck > 0)
                            toScroll.Luck = (short)Math.Max(0, toScroll.Luck + r.Next(1, 6) * inc);
                        if (toScroll.HP > 0)
                            toScroll.HP = (short)Math.Max(0, toScroll.HP + r.Next(1, 6) * inc);
                        if (toScroll.MP > 0)
                            toScroll.MP = (short)Math.Max(0, toScroll.MP + r.Next(1, 6) * inc);
                        if (toScroll.WeaponAttack > 0)
                            toScroll.WeaponAttack = (short)Math.Max(0, toScroll.WeaponAttack + r.Next(1, 6) * inc);
                        if (toScroll.MagicAttack > 0)
                            toScroll.MagicAttack = (short)Math.Max(0, toScroll.MagicAttack + r.Next(1, 6) * inc);
                        if (toScroll.WeaponDefense > 0)
                            toScroll.WeaponDefense = (short)Math.Max(0, toScroll.WeaponDefense + r.Next(1, 6) * inc);
                        if (toScroll.MagicDefense > 0)
                            toScroll.MagicDefense = (short)Math.Max(0, toScroll.MagicDefense + r.Next(1, 6) * inc);
                        if (toScroll.Accuracy > 0)
                            toScroll.Accuracy = (short)Math.Max(0, toScroll.Accuracy + r.Next(1, 6) * inc);
                        if (toScroll.Avoidability > 0)
                            toScroll.Avoidability = (short)Math.Max(0, toScroll.Avoidability + r.Next(1, 6) * inc);
                        if (toScroll.Jump > 0)
                            toScroll.Jump = (short)Math.Max(0, toScroll.Jump + r.Next(1, 6) * inc);
                        if (toScroll.Speed > 0)
                            toScroll.Speed = (short)Math.Max(0, toScroll.Speed + r.Next(1, 6) * inc);
                        toScroll.UpgradesApplied++;
                        toScroll.UpgradesAvailable--;
                        break;
                    case "recover_slot":
                        if (toScroll.UpgradesApplied + toScroll.UpgradesAvailable < toScroll.MaxUpgradesAvailable)
                            toScroll.UpgradesAvailable++;
                        break;
                    case "warm_support":
                        if (toScroll.PreventsColdness == true)
                            return;
                        toScroll.PreventsColdness = true;
                        break;
                    case "prevent_slip":
                        if (toScroll.PreventsSlipping == true)
                            return;
                        toScroll.PreventsSlipping = true;
                        break;
                }
            }
            else
            {
                rand = r.Next(1, 101);
                if (rand <= scroll.BreakItem && scroll.BreakItem > 0)
                {
                    scrollSuccess = ScrollResult.Curse;
                }
                else
                {
                    if (toScroll.PreventsColdness && scroll.Flag.Equals("warm_support") || toScroll.PreventsSlipping && scroll.Flag.Equals("prevent_slip"))
                        return;
                    else if (!whiteScroll && !scroll.Flag.Equals("warm_support") && !scroll.Flag.Equals("prevent_slip") && !scroll.Flag.Equals("recover_slot"))
                        toScroll.UpgradesAvailable--;
                    scrollSuccess = ScrollResult.Fail;
                }
            }
            bool scrollsLeft = true;
            short scrollSlot = scroll.Slot;
            if (scroll.Quantity > 1)
            {
                this.Items.Remove(scroll.MapleID, 1);
            }
            else
            {
                scrollsLeft = false;
                this.Items.Remove(scroll, false);
            }
            if (whiteScroll)
            {
                Item white = this.Items[ItemType.Usable, 2340000];
                if (white.Quantity > 1)
                    this.Items.Remove(2340000, 1);
                else
                    this.Items.Remove(white, true);
            }
            if (scrollSuccess == ScrollResult.Curse)
            {
                using (Packet outPacket = new Packet(MapleServerOperationCode.ModifyInventoryItem))
                {
                    outPacket.WriteByte(1);
                    outPacket.WriteByte(2);
                    outPacket.WriteByte(scrollsLeft ? (byte)1 : (byte)3);
                    outPacket.WriteByte((byte)ItemType.Usable);
                    outPacket.WriteShort((short)scroll.Slot);
                    if (scrollsLeft)
                    {
                        outPacket.WriteShort(scroll.Quantity);
                    }
                    outPacket.WriteByte(3);
                    outPacket.WriteByte((byte)ItemType.Equipment);
                    outPacket.WriteShort(toScroll.Slot);
                    outPacket.WriteByte(1);
                    this.Client.Send(outPacket);
                }
                this.Items.Remove(toScroll, false);
            }
            else
            {
                using (Packet outPacket = new Packet(MapleServerOperationCode.ModifyInventoryItem))
                {
                    outPacket.WriteByte(1);
                    outPacket.WriteByte(3);
                    outPacket.WriteByte(scrollsLeft ? (byte)1 : (byte)3);
                    outPacket.WriteByte((byte)ItemType.Usable);
                    outPacket.WriteShort(scrollSlot);
                    if (scrollsLeft)
                    {
                        outPacket.WriteShort(scroll.Quantity);
                    }
                    outPacket.WriteByte(3);
                    outPacket.WriteByte((byte)ItemType.Equipment);
                    outPacket.WriteShort(toScroll.Slot);
                    outPacket.WriteByte(0);
                    outPacket.WriteByte((byte)ItemType.Equipment);
                    outPacket.WriteShort(toScroll.Slot);
                    outPacket.WriteBytes(toScroll.ToByteArray(true, true));
                    outPacket.WriteByte(1);
                    this.Client.Send(outPacket);
                }
            }
            using (Packet outPacket = new Packet(MapleServerOperationCode.ShowScrollEffect))
            {
                outPacket.WriteInt(this.ID);
                switch (scrollSuccess)
                {
                    case ScrollResult.Success:
                        outPacket.WriteShort(1);
                        outPacket.WriteShort(legendarySpirit ? (short)1 : (short)0);
                        break;
                    case ScrollResult.Fail:
                        outPacket.WriteShort(0);
                        outPacket.WriteShort(legendarySpirit ? (short)1 : (short)0);
                        break;
                    case ScrollResult.Curse:
                        outPacket.WriteByte(0);
                        outPacket.WriteByte(1);
                        outPacket.WriteShort(legendarySpirit ? (short)1 : (short)0);
                        break;
                }
                this.Map.Broadcast(outPacket);
            }
            this.UpdateLook();
        }

        public void UseSkillBook(Packet inPacket)
        {
            inPacket.ReadInt();
            sbyte slot = (sbyte)inPacket.ReadShort();
            int itemId = inPacket.ReadInt();
            Item skillBook = this.Items[ItemType.Usable, slot];
            if (!skillBook.IsSkillBook)
            {

                return;
            }
            if (!this.Items.Contains(itemId))
                return;
            bool canUse = false;
            bool success = false;
            int skillId = 0;
            foreach (int skillid in skillBook.SkillId)
                if (skillid / 10000 == (short)this.Job)
                    skillId = skillid;
            if (skillId == 0)
                return;
            Skill skill = new Skill(skillId);
            if (skillBook.MapleID / 10000 == 228 && this.Skills.Contains(skill.MapleID))
                return;
            else if (skillBook.MapleID / 10000 == 229 && (!this.Skills.Contains(skill.MapleID) || this.Skills[skill.MapleID].MaxLevel >= skillBook.MasterLevel))
                return;
            if (!this.Skills.Contains(skill) || this.Skills[skill.MapleID].CurrentLevel >= skillBook.RequestSkillLevel || skillBook.RequestSkillLevel == 0)
            {
                canUse = true;
                Random r = new Random();
                if (r.Next(1, 101) <= skillBook.Chance)
                {
                    if (this.Skills.Contains(skill.MapleID))
                        this.Skills[skill.MapleID].MaxLevel = (byte)skillBook.MasterLevel;
                    else
                        this.Skills.Add(new Skill(skillId, 0, (byte)skillBook.MasterLevel));
                    success = true;
                    this.Skills[skill.MapleID].Update();
                }
                else
                    success = false;
                this.Items.Remove(skillBook.MapleID, 1);
            }
            else
                canUse = false;
            using (Packet outPacket = new Packet(MapleServerOperationCode.UseSkillBook))
            {
                outPacket.WriteInt(this.ID);
                outPacket.WriteByte(1);
                outPacket.WriteInt(skill.MapleID);
                outPacket.WriteInt(skillBook.MasterLevel);
                outPacket.WriteByte((byte)(canUse ? 1 : 0));
                outPacket.WriteByte((byte)(success ? 1 : 0));
                this.Client.Send(outPacket);
            }
        }

        public void UseItem(Packet inPacket)
        {
            inPacket.ReadInt();
            sbyte slot = (sbyte)inPacket.ReadShort();
            int itemId = inPacket.ReadInt();
            Item consume = this.Items[ItemType.Usable, slot];
            if (consume.CFlags == "" && consume.CBuffTime == 0 && consume.CEffect == 0 && consume.CMoveTo == 0 && consume.CDropUp.Equals("none"))
            {
                this.CurrentHP = (short)(this.CurrentHP + consume.CHP >= this.MaxHP ? this.MaxHP : this.CurrentHP + consume.CHP);
                this.CurrentMP = (short)(this.CurrentMP + consume.CMP >= this.MaxMP ? this.MaxMP : this.CurrentMP + consume.CMP);
                this.CurrentHP = (this.CurrentHP + ((this.MaxHP * consume.CHPPercentage) / 100)) >= this.MaxHP ? this.MaxHP : (short)(this.CurrentHP + ((this.MaxHP * consume.CHPPercentage) / 100));
                this.CurrentMP = (this.CurrentMP + ((this.MaxMP * consume.CMPPercentage) / 100)) >= this.MaxMP ? this.MaxMP : (short)(this.CurrentMP + ((this.MaxMP * consume.CMPPercentage) / 100));
                this.UpdateStatistics(StatisticType.CurrentHP);
                this.UpdateStatistics(StatisticType.CurrentMP);
                this.Items.Remove(itemId, 1);
            }
            else if (consume.CFlags == "" && consume.CBuffTime != 0 && consume.CEffect == 0 && consume.CMoveTo == 0 && consume.CDropUp.Equals("none"))
            {
                Buff buff = new Buff(this.Buffs, consume, 0);
                buff.End = DateTime.Now.AddMinutes(consume.CBuffTime / 60);
                //Stop timer
                this.Buffs.Add(buff);
                this.Items.Remove(itemId, 1);
            }
        }

        public void ResetStats()
        {
            short availableAp = 0;
            if (availableAp + this.Strength - 4 > short.MaxValue)
                availableAp = short.MaxValue;
            else
                availableAp += (short)(this.Strength - 4);
            this.Strength = 4;
            if (availableAp + this.Dexterity - 4 > short.MaxValue)
                availableAp = short.MaxValue;
            else
                availableAp += (short)(this.Dexterity - 4);
            this.Dexterity = 4;
            if (availableAp + this.Intelligence - 4 > short.MaxValue)
                availableAp = short.MaxValue;
            else
                availableAp += (short)(this.Intelligence - 4);
            this.Intelligence = 4;
            if (availableAp + this.Luck - 4 > short.MaxValue)
                availableAp = short.MaxValue;
            else
                availableAp += (short)(this.Luck - 4);
            this.Luck = 4;
            if (availableAp + this.AvailableAP > short.MaxValue)
                this.AvailableAP = short.MaxValue;
            else
                this.AvailableAP += availableAp;
        }

        public void MaxSkills()
        {
            foreach (int skill in World.CachedSkills.Keys)
            {
                byte masterLevel = (byte)World.CachedSkills[skill].Count;
                if (!this.Skills.Contains(skill))
                {
                    if (masterLevel > 0 && (skill / 1000000 == (short)this.Job / 100))
                        this.Skills.Add(World.CachedSkills[skill][masterLevel]);
                }
                else
                {
                    this.Skills[skill].MaxLevel = masterLevel;
                    this.Skills[skill].CurrentLevel = masterLevel;
                }

                if (masterLevel > 0 && (skill / 1000000 == (short)this.Job / 100))
                    this.Skills[skill].Update();
            }
        }

        public void UpdateBuddies(bool Initialize)
        {
            foreach (Buddy loopBuddy in this.BuddyList.Values)
            {
                if (loopBuddy.IsOnline)
                {
                    Character buddy = World.Characters[loopBuddy.Name];

                    if (buddy.BuddyList.ContainsKey(this.ID))
                    {
                        buddy.BuddyList[this.ID].Channel = (byte)(Initialize ? ChannelServer.ChannelID : 0);
                        buddy.BuddyList.UpdateBuddyChannel(buddy.BuddyList[this.ID]);
                    }
                }
            }
        }

        public void BuddyListModify(Packet inPacket)
        {
        }
    }
}