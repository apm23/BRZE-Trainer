from pathlib import Path

p=Path('UnitChangerCore.cs')
s=p.read_text(encoding='utf-8')

def between(start,end,new):
    global s
    a=s.find(start)
    if a<0: raise SystemExit('V14 missing start marker: '+start)
    b=s.find(end,a)
    if b<0: raise SystemExit('V14 missing end marker: '+end)
    s=s[:a]+new+s[b:]

# Convert the V5/V13 single global 9-slot configuration into twelve independent
# native-training-building profiles.  We identify the building by a vanilla
# UnitIn -> UnitOut fingerprint through the already-proven native mapper instead
# of guessing/hard-coding Building type IDs.
config=r'''    const int PROFILE_COUNT=12,SLOT_COUNT=9;
    const int CFG_MASTER=0x00,CFG_PROFILE_BASE=0x100,CFG_PROFILE_STRIDE=0x80,CFG_PROFILE_ACTIVE=0x00,CFG_SLOT_BASE=0x10,CFG_SLOT_STRIDE=8;
    const int T_COMPLETIONS=0x800,T_EXTRA_ATTEMPTS=0x804,T_EXTRA_SUCCESS=0x808,T_EXTRA_FAIL=0x80C,T_BUILDING=0x810,T_INPUT=0x814,T_NATIVE=0x818,T_PENDING=0x81C,T_PRIMARY=0x820,T_LAST_UNIT=0x824,T_PROFILE=0x828,T_PROFILE_PTR=0x82C,T_SLOT_SUCCESS_BASE=0x840,T_PROFILE_HIT_BASE=0x880;
    const int STUB1_OFFSET=0x1000,STUB2_OFFSET=0x3000;

    // Profiles: Dragon Dojo, Target Range, Alchemist Hut;
    // Serpent Tavern, Sharpshooter's Guild, Alchemist Hut;
    // Lotus Forge, Blade Garden, Training Yard;
    // Wolf Combat Pit, Ballistics Grounds, Quarry.
    // Fingerprints are the native first-tier peasant recipes.
    static readonly uint[] ProfileInput={5,5,5,24,24,24,40,40,40,51,51,51};
    static readonly uint[] ProfileNativeOut={8,0,1,29,21,23,30,38,41,46,48,49};

    static readonly object sync=new(); static Process? process; static IntPtr h; static long moduleBase; static IntPtr cave;
    static bool installed; static byte[]? patch1,patch2; static string status="not attached"; static DateTime lastTry;
    static bool wantedMaster; static readonly bool[,] wantedEn=new bool[PROFILE_COUNT,SLOT_COUNT]; static readonly uint[,] wantedOut=new uint[PROFILE_COUNT,SLOT_COUNT];

    static long ProfileBase(long cfg,int p)=>cfg+CFG_PROFILE_BASE+p*CFG_PROFILE_STRIDE;
    static long SlotEn(long cfg,int p,int i)=>ProfileBase(cfg,p)+CFG_SLOT_BASE+i*CFG_SLOT_STRIDE;
    static long SlotOut(long cfg,int p,int i)=>SlotEn(cfg,p,i)+4;
    static long SlotSuccess(long cfg,int i)=>cfg+T_SLOT_SUCCESS_BASE+i*4;
    static long ProfileHit(long cfg,int p)=>cfg+T_PROFILE_HIT_BASE+p*4;
    static int ProfileSlotOffset(int i)=>CFG_SLOT_BASE+i*CFG_SLOT_STRIDE;
    static int ProfileOutOffset(int i)=>ProfileSlotOffset(i)+4;

    public static void ConfigureProfiles(bool[,] enabled,uint[,] outputs)
    {
        lock(sync)
        {
            wantedMaster=false;
            for(int p=0;p<PROFILE_COUNT;p++)
            {
                for(int i=0;i<SLOT_COUNT;i++)
                {
                    bool e=enabled.GetLength(0)>p&&enabled.GetLength(1)>i&&enabled[p,i];
                    uint o=outputs.GetLength(0)>p&&outputs.GetLength(1)>i?outputs[p,i]:0u;
                    wantedEn[p,i]=e;wantedOut[p,i]=o;if(e)wantedMaster=true;
                }
            }
            if(!EnsureAttached()||cave==IntPtr.Zero)return;PushConfig();
        }
    }

    static void PushConfig()
    {
        long c=cave.ToInt64();W32(c+CFG_MASTER,wantedMaster?1u:0u);
        for(int p=0;p<PROFILE_COUNT;p++)
        {
            bool active=false;for(int i=0;i<SLOT_COUNT;i++)if(wantedEn[p,i]){active=true;break;}
            W32(ProfileBase(c,p)+CFG_PROFILE_ACTIVE,active?1u:0u);
            for(int i=0;i<SLOT_COUNT;i++){W32(SlotOut(c,p,i),wantedOut[p,i]);W32(SlotEn(c,p,i),wantedEn[p,i]?1u:0u);}
        }
    }

    public static void ResetCounters()
    {
        lock(sync)
        {
            if(!EnsureAttached()||cave==IntPtr.Zero)return;long c=cave.ToInt64();
            foreach(int o in new[]{T_COMPLETIONS,T_EXTRA_ATTEMPTS,T_EXTRA_SUCCESS,T_EXTRA_FAIL,T_BUILDING,T_PENDING,T_PRIMARY,T_LAST_UNIT,T_PROFILE,T_PROFILE_PTR})W32(c+o,0);
            W32(c+T_INPUT,0xFFFFFFFF);W32(c+T_NATIVE,0xFFFFFFFF);
            for(int i=0;i<SLOT_COUNT;i++)W32(SlotSuccess(c,i),0);
            for(int p=0;p<PROFILE_COUNT;p++)W32(ProfileHit(c,p),0);
        }
    }
'''
between('    const int CFG_MASTER=','    public static void Reset(){',config+'    public static void Reset(){')

completion=r'''    static byte[] BuildCompletionStub(long stub,long cfg)
    {
        long mapper=moduleBase+RVA_TRAIN_MAP,pending=cfg+T_PENDING,primary=cfg+T_PRIMARY,lb=cfg+T_BUILDING,li=cfg+T_INPUT,ln=cfg+T_NATIVE,n=cfg+T_COMPLETIONS,lp=cfg+T_PROFILE,lpp=cfg+T_PROFILE_PTR;
        var b=new List<byte>();
        b.AddRange(new byte[]{0xC7,0x05});U32(b,(uint)pending);U32(b,0); // pending=0 every attempt
        b.Add(0x52);                                      // preserve caller EDX
        b.AddRange(new byte[]{0x8B,0xD1});               // EDX = Building*
        b.AddRange(new byte[]{0xFF,0x74,0x24,0x08});     // original input UnitType
        b.Add(0xE8);int callMap=b.Count;I32(b,0);         // native UnitIn -> UnitOut mapper
        b.AddRange(new byte[]{0x83,0x3D});U32(b,(uint)(cfg+CFG_MASTER));b.Add(0x00);b.AddRange(new byte[]{0x0F,0x84});int jNativeMaster=b.Count;I32(b,0);
        b.AddRange(new byte[]{0x83,0xF8,0xFF});b.AddRange(new byte[]{0x0F,0x84});int jNativeMap=b.Count;I32(b,0);
        b.Add(0x8B);b.Add(0x0D);U32(b,(uint)(moduleBase+RVA_LOCAL_ID));b.AddRange(new byte[]{0x39,0x8A,0x84,0x00,0x00,0x00});b.AddRange(new byte[]{0x0F,0x85});int jNativeOwner=b.Count;I32(b,0);
        b.Add(0xA3);U32(b,(uint)ln);                       // keep the true native output while fingerprint mapper calls clobber EAX

        var profileCalls=new List<int>();var profileJumps=new List<int>();
        for(int p=0;p<PROFILE_COUNT;p++)
        {
            b.Add(0x68);U32(b,ProfileInput[p]);           // push fingerprint UnitIn
            b.AddRange(new byte[]{0x8B,0xCA});            // ECX = Building*
            b.Add(0xE8);int c=b.Count;I32(b,0);profileCalls.Add(c);
            b.Add(0x3D);U32(b,ProfileNativeOut[p]);       // exact vanilla UnitOut fingerprint
            b.AddRange(new byte[]{0x0F,0x84});int j=b.Count;I32(b,0);profileJumps.Add(j);
        }
        b.Add(0xA1);U32(b,(uint)ln);                      // unmatched normal building -> native output
        b.Add(0xE9);int jNoProfile=b.Count;I32(b,0);

        var profileLabels=new int[PROFILE_COUNT];var toScan=new List<int>();
        for(int p=0;p<PROFILE_COUNT;p++)
        {
            profileLabels[p]=b.Count;
            b.AddRange(new byte[]{0xC7,0x05});U32(b,(uint)lp);U32(b,(uint)p);
            b.AddRange(new byte[]{0xC7,0x05});U32(b,(uint)lpp);U32(b,(uint)ProfileBase(cfg,p));
            b.AddRange(new byte[]{0xFF,0x05});U32(b,(uint)ProfileHit(cfg,p));
            b.Add(0xE9);int j=b.Count;I32(b,0);toScan.Add(j);
        }

        int scan=b.Count;
        b.Add(0xA1);U32(b,(uint)lpp);                     // EAX = active profile config
        b.AddRange(new byte[]{0x83,0x38,0x00});           // profile enabled?
        b.AddRange(new byte[]{0x0F,0x84});int jProfileOff=b.Count;I32(b,0);
        var slotJumps=new List<int>();
        for(int i=0;i<SLOT_COUNT;i++)
        {
            b.Add(0xA1);U32(b,(uint)lpp);
            b.AddRange(new byte[]{0x83,0xB8});I32(b,ProfileSlotOffset(i));b.Add(0x00);
            b.AddRange(new byte[]{0x0F,0x85});int j=b.Count;I32(b,0);slotJumps.Add(j);
        }
        b.Add(0xE9);int jNoActive=b.Count;I32(b,0);

        var slotLabels=new int[SLOT_COUNT];
        for(int i=0;i<SLOT_COUNT;i++)
        {
            slotLabels[i]=b.Count;
            b.AddRange(new byte[]{0x89,0x15});U32(b,(uint)lb);b.AddRange(new byte[]{0x8B,0x4C,0x24,0x08});b.AddRange(new byte[]{0x89,0x0D});U32(b,(uint)li);
            b.AddRange(new byte[]{0xFF,0x05});U32(b,(uint)n);b.AddRange(new byte[]{0xC7,0x05});U32(b,(uint)pending);U32(b,1);b.AddRange(new byte[]{0xC7,0x05});U32(b,(uint)primary);U32(b,(uint)i);
            b.Add(0xA1);U32(b,(uint)lpp);b.AddRange(new byte[]{0x8B,0x80});I32(b,ProfileOutOffset(i));
            b.Add(0xE9);int jf=b.Count;I32(b,0);slotJumps.Add(jf);
        }

        int nativeRestore=b.Count;b.Add(0xA1);U32(b,(uint)ln);
        int finish=b.Count;b.AddRange(new byte[]{0x8B,0x4C,0x24,0x08});b.Add(0x5A);b.AddRange(new byte[]{0xC2,0x04,0x00});

        PatchRel(b,callMap,stub+callMap+4,mapper);
        foreach(int c in profileCalls)PatchRel(b,c,stub+c+4,mapper);
        PatchRel(b,jNativeMaster,stub+jNativeMaster+4,stub+finish);PatchRel(b,jNativeMap,stub+jNativeMap+4,stub+finish);PatchRel(b,jNativeOwner,stub+jNativeOwner+4,stub+finish);
        for(int p=0;p<PROFILE_COUNT;p++)PatchRel(b,profileJumps[p],stub+profileJumps[p]+4,stub+profileLabels[p]);
        PatchRel(b,jNoProfile,stub+jNoProfile+4,stub+finish);
        foreach(int j in toScan)PatchRel(b,j,stub+j+4,stub+scan);
        PatchRel(b,jProfileOff,stub+jProfileOff+4,stub+nativeRestore);PatchRel(b,jNoActive,stub+jNoActive+4,stub+nativeRestore);
        for(int i=0;i<SLOT_COUNT;i++)PatchRel(b,slotJumps[i],stub+slotJumps[i]+4,stub+slotLabels[i]);
        for(int i=SLOT_COUNT;i<slotJumps.Count;i++)PatchRel(b,slotJumps[i],stub+slotJumps[i]+4,stub+finish);
        return b.ToArray();
    }

'''
between('    static byte[] BuildCompletionStub(long stub,long cfg)','    static byte[] BuildExtrasStub(long stub,long cfg)',completion)

extras=r'''    static byte[] BuildExtrasStub(long stub,long cfg)
    {
        long original=moduleBase+RVA_REGISTER_UNIT,create=moduleBase+RVA_CREATE_UNIT,attach=moduleBase+RVA_ATTACH_UNIT,notify=moduleBase+RVA_LOCAL_UNIT_NOTIFY;
        long pending=cfg+T_PENDING,expectedBuilding=cfg+T_BUILDING,primary=cfg+T_PRIMARY,attempts=cfg+T_EXTRA_ATTEMPTS,success=cfg+T_EXTRA_SUCCESS,fail=cfg+T_EXTRA_FAIL,lastUnit=cfg+T_LAST_UNIT,profilePtr=cfg+T_PROFILE_PTR;
        var b=new List<byte>();
        b.AddRange(new byte[]{0xFF,0x74,0x24,0x08});b.AddRange(new byte[]{0xFF,0x74,0x24,0x08});b.Add(0xE8);int callOriginal=b.Count;I32(b,0);
        b.AddRange(new byte[]{0x83,0x3D});U32(b,(uint)pending);b.Add(0x01);b.AddRange(new byte[]{0x0F,0x85});int jFinish0=b.Count;I32(b,0);
        b.AddRange(new byte[]{0x3B,0x1D});U32(b,(uint)expectedBuilding);b.AddRange(new byte[]{0x0F,0x85});int jFinish1=b.Count;I32(b,0);
        b.AddRange(new byte[]{0xC7,0x05});U32(b,(uint)pending);U32(b,0);
        b.Add(0x60);b.AddRange(new byte[]{0x83,0xEC,0x40});
        b.AddRange(new byte[]{0x0F,0x11,0x04,0x24,0x0F,0x11,0x4C,0x24,0x10,0x0F,0x11,0x54,0x24,0x20,0x0F,0x11,0x5C,0x24,0x30});

        var toBlockEnd=new List<(int at,int endId)>();var blockEnds=new List<int>();
        for(int i=0;i<SLOT_COUNT;i++)
        {
            int id=i;
            b.Add(0xA1);U32(b,(uint)profilePtr);b.AddRange(new byte[]{0x83,0xB8});I32(b,ProfileSlotOffset(i));b.Add(0x00);b.AddRange(new byte[]{0x0F,0x84});int jDisabled=b.Count;I32(b,0);
            b.AddRange(new byte[]{0x83,0x3D});U32(b,(uint)primary);b.Add((byte)i);b.AddRange(new byte[]{0x0F,0x84});int jPrimary=b.Count;I32(b,0);
            b.AddRange(new byte[]{0xFF,0x05});U32(b,(uint)attempts);
            b.Add(0x6A);b.Add(0x01);b.AddRange(new byte[]{0xFF,0xB3,0x84,0x00,0x00,0x00});
            b.Add(0xA1);U32(b,(uint)profilePtr);b.AddRange(new byte[]{0xFF,0xB0});I32(b,ProfileOutOffset(i));
            b.AddRange(new byte[]{0x8B,0xCB});b.Add(0xE8);int callCreate=b.Count;I32(b,0);
            b.AddRange(new byte[]{0x85,0xC0});b.AddRange(new byte[]{0x0F,0x85});int jCreated=b.Count;I32(b,0);
            b.AddRange(new byte[]{0xFF,0x05});U32(b,(uint)fail);b.Add(0xE9);int jFailEnd=b.Count;I32(b,0);
            int created=b.Count;b.AddRange(new byte[]{0x89,0xC6});b.Add(0xA3);U32(b,(uint)lastUnit);b.AddRange(new byte[]{0xFF,0x05});U32(b,(uint)success);b.AddRange(new byte[]{0xFF,0x05});U32(b,(uint)SlotSuccess(cfg,i));
            b.Add(0x56);b.AddRange(new byte[]{0x8B,0xCB});b.Add(0xE8);int callAttach=b.Count;I32(b,0);
            b.AddRange(new byte[]{0x8B,0x83,0xA8,0x04,0x00,0x00,0x89,0x86,0x6C,0x03,0x00,0x00,0x8B,0x83,0xAC,0x04,0x00,0x00,0x89,0x86,0x70,0x03,0x00,0x00});
            b.AddRange(new byte[]{0x69,0x8B,0x84,0x00,0x00,0x00,0xA0,0x00,0x00,0x00});b.Add(0xA1);U32(b,(uint)(moduleBase+RVA_PLAYER_BASE));b.AddRange(new byte[]{0xFF,0x44,0x01,0x28});
            b.AddRange(new byte[]{0x8B,0x83,0x84,0x00,0x00,0x00});b.Add(0x3B);b.Add(0x05);U32(b,(uint)(moduleBase+RVA_LOCAL_ID));b.AddRange(new byte[]{0x0F,0x85});int jSkipNotify=b.Count;I32(b,0);
            b.AddRange(new byte[]{0x8B,0x46,0x74,0xFF,0x30});b.Add(0xE8);int callNotify=b.Count;I32(b,0);int afterNotify=b.Count;
            b.AddRange(new byte[]{0x8B,0x83,0xA0,0x04,0x00,0x00,0x89,0x86,0x98,0x07,0x00,0x00,0x69,0x83,0x84,0x00,0x00,0x00,0xA0,0x00,0x00,0x00});b.Add(0x56);b.Add(0x50);b.Add(0xE8);int callReg=b.Count;I32(b,0);
            int blockEnd=b.Count;blockEnds.Add(blockEnd);
            PatchRel(b,jCreated,stub+jCreated+4,stub+created);PatchRel(b,jSkipNotify,stub+jSkipNotify+4,stub+afterNotify);PatchRel(b,callCreate,stub+callCreate+4,create);PatchRel(b,callAttach,stub+callAttach+4,attach);PatchRel(b,callNotify,stub+callNotify+4,notify);PatchRel(b,callReg,stub+callReg+4,original);
            toBlockEnd.Add((jDisabled,id));toBlockEnd.Add((jPrimary,id));toBlockEnd.Add((jFailEnd,id));
        }
        int restore=b.Count;b.AddRange(new byte[]{0x0F,0x10,0x04,0x24,0x0F,0x10,0x4C,0x24,0x10,0x0F,0x10,0x54,0x24,0x20,0x0F,0x10,0x5C,0x24,0x30,0x83,0xC4,0x40});b.Add(0x61);
        int finish=b.Count;b.AddRange(new byte[]{0xC2,0x08,0x00});
        PatchRel(b,callOriginal,stub+callOriginal+4,original);PatchRel(b,jFinish0,stub+jFinish0+4,stub+finish);PatchRel(b,jFinish1,stub+jFinish1+4,stub+finish);
        foreach(var x in toBlockEnd)PatchRel(b,x.at,stub+x.at+4,stub+blockEnds[x.endId]);return b.ToArray();
    }

'''
between('    static byte[] BuildExtrasStub(long stub,long cfg)','    static byte[] CallPatch(long target,long stub)',extras)

s=s.replace('VirtualAllocEx(h,IntPtr.Zero,(UIntPtr)8192,MEM_COMMIT|MEM_RESERVE,PAGE_EXECUTE_READWRITE)','VirtualAllocEx(h,IntPtr.Zero,(UIntPtr)32768,MEM_COMMIT|MEM_RESERVE,PAGE_EXECUTE_READWRITE)')
s=s.replace('WriteBytes(c,new byte[512]);PushConfig();','WriteBytes(c,new byte[4096]);PushConfig();')
s=s.replace('if(code2.Length>5500)','if(code2.Length>12000)')
s=s.replace('status="V5 FULL 1→9 HOOK ACTIVE — V3/V4 proven paths generalized";','status="V14 PROFILED 1→9 HOOK ACTIVE — 12 native training-building fingerprints";')

monitor=r'''    static string Monitor()
    {
        if(!installed||cave==IntPtr.Zero)return status+"\r\nNo profiled multi-output override active.";
        long c=cave.ToInt64();uint n=R32(c+T_COMPLETIONS),a=R32(c+T_EXTRA_ATTEMPTS),ok=R32(c+T_EXTRA_SUCCESS),f=R32(c+T_EXTRA_FAIL),b=R32(c+T_BUILDING),i=R32(c+T_INPUT),nat=R32(c+T_NATIVE),pend=R32(c+T_PENDING),primary=R32(c+T_PRIMARY),last=R32(c+T_LAST_UNIT),profile=R32(c+T_PROFILE);
        var slots=new StringBuilder();for(int x=0;x<SLOT_COUNT;x++){if(x>0)slots.Append(' ');slots.Append($"S{x+1}:{R32(SlotSuccess(c,x))}");}
        int activeProfiles=0,activeSlots=0;for(int p=0;p<PROFILE_COUNT;p++){if(R32(ProfileBase(c,p))!=0)activeProfiles++;for(int x=0;x<SLOT_COUNT;x++)if(R32(SlotEn(c,p,x))!=0)activeSlots++;}
        var hits=new StringBuilder();for(int p=0;p<PROFILE_COUNT;p++){if(p>0)hits.Append(' ');hits.Append($"P{p+1}:{R32(ProfileHit(c,p))}");}
        return $"{status}\r\nprofiles:{activeProfiles} active slots:{activeSlots} | last profile:P{profile+1} primary:S{primary+1} | valid completions:{n}\r\nextra attempts:{a} success:{ok} fail:{f} | pending:{pend} | last extra unit:0x{last:X8}\r\nslot successes: {slots}\r\nprofile hits: {hits}\r\nlast completion: building:0x{b:X8} input:0x{i:X8} nativeOut:0x{nat:X8}\r\nGuard: vanilla mapper fingerprint + local owner; unmatched buildings remain native.";
    }
'''
between('    static string Monitor()','    public static CoreSnapshot Snapshot()',monitor)

p.write_text(s,encoding='utf-8')
print('V14 UnitChangerCore: 12 independent native-building profiles generated')
