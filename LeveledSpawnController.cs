using Server;
using System;
using System.Collections.Generic;
using System.Linq;
using Server.Mobiles;
using Server.Gumps;
using Server.Targeting;
using Server.Targets;
using Server.Network;

namespace Server.Items
{
    public class LeveledSpawnController : Item
    {
	private bool IsActive;
	private List<Spawner> m_Spawners;
	private InternalTimer m_Timer;

	private int m_CurrentLevel;
	private int m_MaxLevel;
	private int m_Remaining;
	private int m_Cooldown;

        [CommandProperty(AccessLevel.Spawner)]
	public bool Active
	{
	    get { return IsActive; }
	    set { IsActive = value; }
	}

        [CommandProperty(AccessLevel.Spawner)]
	public List<Spawner> Spawners
	{
	    get { return m_Spawners; }
	    set { m_Spawners = value; }
	}

        [CommandProperty(AccessLevel.Spawner)]
	public int CurrentLevel
	{
	    get { return m_CurrentLevel; }
	    set { m_CurrentLevel = value; }
	}

        [CommandProperty(AccessLevel.Spawner)]
	public int MaxLevel
	{
	    get { return m_MaxLevel; }
	    set { m_MaxLevel = value; }
	}

        [CommandProperty(AccessLevel.Spawner)]
	public int Remaining
	{
	    get { return m_Remaining; }
	    set { m_Remaining = value; }
	}

        [CommandProperty(AccessLevel.Spawner)]
	public int Cooldown
	{
	    get { return m_Cooldown; }
	    set { m_Cooldown = value; }
	}

	[Constructable]
	public LeveledSpawnController() : base (0xA25)
	{
	    List<Spawner> spawners = new List<Spawner>();
	    Name = "Leveled Spawn Controller";
	    Visible = false;
	    m_Spawners = spawners;
	    m_Cooldown = 45;
	    IsActive = false;
	    Movable = false;
	}

	public override void OnDoubleClick(Mobile from)
	{
	    if (!from.HasGump(typeof(SpawnControllerGump)))
		from.SendGump(new SpawnControllerGump(from, this));

	}

	public void Activate()
	{
	    if (!IsActive)
	    {
		IsActive = true;
		m_CurrentLevel = 1;

		m_Timer = new InternalTimer(this);
		m_Timer.Start();

		if (m_Spawners == null)
		    m_Spawners = new List<Spawner>();

		m_MaxLevel = m_Spawners.Count;

		if (m_Spawners.Count > 0)
		{
	    	    m_Spawners[0].Running = true;
		    m_Spawners[0].Respawn();
		}
	    }
	}

	public void Deactivate()
	{
	    if (IsActive)
	    {
		IsActive = false;
		m_CurrentLevel = 1;

		if (m_Timer != null)
		{
		    m_Timer.Stop();
		    m_Timer = null;
		}

		if (m_Spawners.Count > 0)
		{
		    for(int i = 0; i < m_Spawners.Count; i++)
		    {
			m_Spawners[i].Running = false;
			m_Spawners[i].RemoveSpawned();
		    }
		}	
	    }
	}

	public void Reset()
	{

	    if (IsActive)
	    	Deactivate();

	    PublicOverheadMessage(MessageType.Regular, 0, false, "Reset in " + m_Cooldown.ToString() + " seconds.");

	    Timer.DelayCall(TimeSpan.FromSeconds(m_Cooldown), () => 
	    {
		Activate();    
	    });
	}

	public void Restart()
	{
	    if (IsActive)
	    {
	    	Deactivate();

	    	Timer.DelayCall(TimeSpan.FromSeconds(5), () => 
	    	{
	    	    Activate();
	    	});
	    }
	}

	public void NextLevel()
	{
	    if (m_CurrentLevel < m_MaxLevel)
	    {
	    	m_Spawners[m_CurrentLevel-1].Running = false;
	    	m_Spawners[m_CurrentLevel].Running = true;
	    	m_Spawners[m_CurrentLevel].Respawn();
	    }

	    m_CurrentLevel ++;
	}

	public LeveledSpawnController(Serial serial) : base (serial)
	{
	}

	public override void Serialize(GenericWriter writer)
	{
	    base.Serialize(writer);
	    writer.Write((int)0);

	    if(m_Spawners != null)
	    {
		writer.Write(m_Spawners.Count);
		for( int i = 0; i < m_Spawners.Count; i++)
		{
		    writer.Write(m_Spawners[i]);
		}
	    }
	    else
	    {
		writer.Write((int)0);
	    }

	    writer.Write(m_CurrentLevel);
	    writer.Write(m_MaxLevel);
	    writer.Write(m_Remaining);
	    writer.Write(m_Cooldown);
	    writer.Write(IsActive);

	}

	public override void Deserialize(GenericReader reader)
	{
	    base.Deserialize(reader);
	    int version = reader.ReadInt();

	    int size = reader.ReadInt();
	    m_Spawners = new List<Spawner>(size);
	    for( int i = 0; i < size; i++)
	    {
		Spawner spawner = reader.ReadItem() as Spawner;
		m_Spawners.Add(spawner);
	    }

	    m_CurrentLevel = reader.ReadInt();
	    m_MaxLevel = reader.ReadInt();
	    m_Remaining = reader.ReadInt();
	    m_Cooldown = reader.ReadInt();
	    IsActive = reader.ReadBool();

	    Restart();
	}

	private class InternalTimer : Timer
	{
	    private LeveledSpawnController Controller;

	    public InternalTimer(LeveledSpawnController controller)
		: base(TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(1.0))
	    {
		Controller = controller;
	    }

	    protected override void OnTick()
	    {
		if (Controller.Deleted || !Controller.Active)
		    Stop();

		Controller.Remaining = Controller.Spawners[Controller.CurrentLevel-1].SpawnCount;

		if (Controller.CurrentLevel <= Controller.MaxLevel)
		{
		    if (Controller.Remaining == 0)
		    {
			Controller.NextLevel();
		    }
		}

		if (Controller.CurrentLevel > Controller.MaxLevel && Controller.Remaining == 0)
		{
		    Controller.Deactivate();
		    Controller.Reset();
		}
	    }
	}
    }

    public class SpawnControllerGump : Gump
    {
	private LeveledSpawnController Controller;
	private Mobile From;
	private List<Spawner> Spawners;

	public SpawnControllerGump(Mobile from, LeveledSpawnController controller)
	    : base(100, 100)
	{
	    From = from;
	    Controller = controller;
	    Spawners = controller.Spawners;

	    AddGumpLayout();
	}

	public void AddGumpLayout()
	{
	    AddBackground(0, 0, 350, 555, 0x6DB);

	    AddHtml(10, 10, 190, 16, FormatOptions("CONTROLLER OPTIONS:", "#F0F8FF"), false, false);

	    if (Spawners != null)
	    {
		int y = 100;
		AddHtml(30, 40, 400, 16, FormatOptions("Levels: ",  "#F0F8FF"), false, false);
		AddHtml(90, 40, 400, 16, FormatOptions(Spawners.Count.ToString(),  "#F0F8FF"), false, false);

		AddHtml(50, 70, 400, 16, FormatOptions("Name",  "#F0F8FF"), false, false);
		AddHtml(130, 70, 400, 16, FormatOptions("Ctrl",  "#F0F8FF"), false, false);
		AddHtml(210, 70, 400, 16, FormatOptions("Props",  "#F0F8FF"), false, false);
		AddHtml(290, 70, 400, 16, FormatOptions("Go To",  "#F0F8FF"), false, false);

		for(int i = 0; i < Spawners.Count; i++)
		{
		    AddHtml(50, y, 400, 16, FormatOptions(Spawners[i].Name, "#F0F8FF"), false, false);
            	    AddButton(130, y, 0xFA8, 0xFAB, 10 + i, GumpButtonType.Reply, 0);
		    AddButton(210, y, 0xFAB, 0xFAD, 20 + i, GumpButtonType.Reply, 0);
            	    AddButton(290, y, 0xFB1, 0xFB3, 30 + i, GumpButtonType.Reply, 0);

		    if (i > 0 )
	            	AddButton(20, y-5, 0x983, 0x984, 40 + i, GumpButtonType.Reply, 0); //up

		    if (i < (Spawners.Count - 1))
            	    	AddButton(20, y+5, 0x985, 0x986, 50 + i, GumpButtonType.Reply, 0); //down  

		    y += 30;    
		}

		
	    }

	    AddHtml(55, 490, 400, 16, FormatOptions(Controller.Active ? "Active" : "Inactive", Controller.Active ?  "#50C878" : "#FF0000"), false, false);
	    AddButton(15, 485, Controller.Active ? 0x16C2 : 0x16C6, Controller.Active ? 0x16C6 : 0x16C2, 2, GumpButtonType.Reply, 0);

	    AddHtml(55, 520, 400, 16, FormatOptions("Add Spawner",  "#F0F8FF"), false, false);
            AddButton(15, 520, 0xFB7, 0xFB9, 1, GumpButtonType.Reply, 0);
	}

	public string FormatOptions(string val, string color)
	{
	    if( color == null)
		return String.Format("<div align=left>{0}</div>", val);
	    else
	    	return String.Format("<BASEFONT COLOR={1}><dic align=left>{0}</div>", val, color);
	}

        public override void OnResponse(NetState sender, RelayInfo info)
        {
	    if (info.ButtonID == 1)
	    {
		From.Target = new SpawnControllerTarget(Controller);
	    }

	    if (info.ButtonID == 2)
	    {
		if (!Controller.Active)
		    Controller.Activate();
		else
		    Controller.Deactivate();	

		From.SendGump(new SpawnControllerGump(From, Controller));
	    }

	    if (info.ButtonID > 9 && info.ButtonID < 20)
	    {
		BaseGump.SendGump(new SpawnerGump(From, Controller.Spawners[info.ButtonID - 10]));
		//From.SendGump(new SpawnerGump(From, Controller.Spawners[info.ButtonID - 10])); // for older version without BaseGump
		From.SendGump(new SpawnControllerGump(From, Controller));
	    }

	    if (info.ButtonID > 19 && info.ButtonID < 30)
	    {
		From.SendGump(new PropertiesGump(From, Controller.Spawners[info.ButtonID - 20]));
		From.SendGump(new SpawnControllerGump(From, Controller));
	    }

	    if (info.ButtonID > 29 && info.ButtonID < 40)
	    {
		Item item = Controller.Spawners[info.ButtonID - 30];
		From.MoveToWorld(item.Location, item.Map);
		From.SendGump(new SpawnControllerGump(From, Controller));
	    }

	    if (info.ButtonID > 39 && info.ButtonID < 50) //up
	    {
		if (!Controller.Active)
		{
		    Spawner spawner = Controller.Spawners[info.ButtonID - 40];
		    Controller.Spawners.RemoveAt(info.ButtonID - 40);
		    Controller.Spawners.Insert(info.ButtonID - 41, spawner);
		    From.SendGump(new SpawnControllerGump(From, Controller));
		}
		else
		    From.SendMessage("You cannot do that while the Controller is active.");
	    }

	    if (info.ButtonID > 49 && info.ButtonID < 60) //down
	    {
		if (!Controller.Active)
		{
		    Spawner spawner = Controller.Spawners[info.ButtonID - 50];
		    Controller.Spawners.RemoveAt(info.ButtonID - 50);
		    Controller.Spawners.Insert(info.ButtonID - 49, spawner);
		    From.SendGump(new SpawnControllerGump(From, Controller));
		}
		else
		    From.SendMessage("You cannot do that while the Controller is active.");
	    }
	}
    }

    public class SpawnControllerTarget : Target
    {
	private LeveledSpawnController Controller;

	public SpawnControllerTarget(LeveledSpawnController controller)
	    : base( 12, false, TargetFlags.None)
	{
	    Controller = controller;
	}

	protected override void OnTarget(Mobile from, object targeted)
	{
	    if (targeted is Spawner)
	    {
		Spawner spawner = (Spawner)targeted;
		spawner.Running = false;
		spawner.Group = true;

	 	if (Controller.Spawners == null)
		    Controller.Spawners = new List<Spawner>();

		if ( !Controller.Spawners.Contains(spawner) && Controller.Spawners.Count < 11)
		    Controller.Spawners.Add(spawner);
		else if(Controller.Spawners.Count >= 11)
		    from.SendMessage("The spawner list is full.");	
	    }

	    from.SendGump(new SpawnControllerGump(from, Controller));
	}
    }
}
