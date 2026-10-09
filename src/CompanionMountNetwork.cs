using System;
using System.Globalization;
using System.Linq;
using Steamworks;
using UnityEngine;

namespace KingdomAdvisor
{
    // 使用独立成员元数据键；原版端不需要处理或注册任何 Mod RPC。
    internal sealed class CompanionMountNetwork
    {
        private const string Key="local.kingdom.advisor.mounts.v1";
        private readonly CompanionMounts mounts;
        private readonly GameActions actions;
        private string session=Guid.NewGuid().ToString("N");
        private float nextPoll;
        private string hostSession="",lastPublished="",remoteSession="";
        private int desiredKind,requestedCast,seenCast,requestedOwner=-1;
        private ulong lobbyId;
        private ulong peerId;
        public bool HostAvailable {get;private set;}
        public bool PeerMod {get;private set;}
        public CompanionMountNetwork(CompanionMounts mounts,GameActions actions){this.mounts=mounts;this.actions=actions;}
        public bool RequestSelection(Snapshot s,int owner,int kind)
        {
            if(!s.Online || NetworkBigBoss.HasWorldAuth)return false;
            if(!HostAvailable){return true;}
            requestedOwner=owner;desiredKind=kind;PublishRequest(s);return true;
        }
        public bool RequestCast(Snapshot s,int owner)
        {
            if(!s.Online || NetworkBigBoss.HasWorldAuth)return false;
            if(!HostAvailable){return true;}
            if(mounts.Selection(owner)==0){return true;}
            requestedOwner=owner;desiredKind=mounts.Selection(owner);requestedCast++;
            PublishRequest(s);return true;
        }
        private void Publish(CSteamID lobby,string value)
        {
            if(value==lastPublished)return;
            SteamMatchmaking.SetLobbyMemberData(lobby,Key,value);lastPublished=value;
        }
        private void PublishRequest(Snapshot s)
        {
            var platform=SteamPlatformManager.Inst;
            if(!platform || !platform.inLobby)return;
            Publish(platform.activeLobbyID,"R|"+hostSession+"|"+s.Island+"|"+requestedOwner+"|"+desiredKind+"|"+requestedCast);
        }
        public void Poll(Snapshot s)
        {
            if(Time.unscaledTime<nextPoll)return;nextPoll=Time.unscaledTime+.25f;
            if(!s.Playing || !s.Online){Reset();return;}
            var platform=SteamPlatformManager.Inst;
            if(!platform || !platform.inLobby || !SteamManager.Initialized){HostAvailable=PeerMod=false;return;}
            var lobby=platform.activeLobbyID;
            if(lobbyId!=lobby.m_SteamID){Reset();lobbyId=lobby.m_SteamID;}
            int count=SteamMatchmaking.GetNumLobbyMembers(lobby);
            if(count!=2){HostAvailable=PeerMod=false;mounts.RemoveRemote();return;}
            var self=SteamUser.GetSteamID();
            var first=SteamMatchmaking.GetLobbyMemberByIndex(lobby,0);
            var peer=first.m_SteamID==self.m_SteamID?SteamMatchmaking.GetLobbyMemberByIndex(lobby,1):first;
            if(peerId!=0 && peerId!=peer.m_SteamID){Reset();lobbyId=lobby.m_SteamID;}
            peerId=peer.m_SteamID;
            string data=SteamMatchmaking.GetLobbyMemberData(lobby,peer,Key)??"";
            if(data.Length>512){HostAvailable=PeerMod=false;return;}
            var fields=data.Split('|');
            if(NetworkBigBoss.HasWorldAuth)
            {
                // 主机也持续声明协议；原版成员未声明时，仅走原生伤害同步。
                PeerMod=fields.Length==6 && fields[0]=="R" && fields[1]==session;
                if(PeerMod && int.TryParse(fields[2],out int remoteIsland) && remoteIsland==s.Island && int.TryParse(fields[3],out int owner) && int.TryParse(fields[4],out int kind) && int.TryParse(fields[5],out int cast) && kind>=0 && kind<=2 && cast>=0)
                {
                    var player=s.Players.FirstOrDefault(p=>p.Id==owner);
                    if(player!=null && !player.Local)
                    {
                        if(remoteSession!=fields[1]){remoteSession=fields[1];seenCast=0;}
                        if(mounts.Selection(owner)!=kind)mounts.Select(s,owner,kind,true);
                        if(cast>seenCast){seenCast=cast;mounts.Cast(s,owner,true);}
                    }
                }
                Publish(lobby,"H|"+session+"|"+s.Island+"|"+mounts.NetworkSlot(0)+"|"+mounts.NetworkSlot(1));
            }
            else
            {
                HostAvailable=fields.Length==5 && fields[0]=="H" && fields[1].Length==32 && int.TryParse(fields[2],out int hostIsland) && hostIsland==s.Island && CompanionMountWire.TrySlot(fields[3],out _) && CompanionMountWire.TrySlot(fields[4],out _);
                if(!HostAvailable)return;
                if(hostSession!=fields[1]){hostSession=fields[1];requestedCast=0;requestedOwner=s.Players.FirstOrDefault(p=>p.Local)?.Id??-1;desiredKind=0;mounts.Restore();}
                PeerMod=true;
                mounts.ApplyNetworkSlot(s,0,fields[3]);mounts.ApplyNetworkSlot(s,1,fields[4]);
                PublishRequest(s);
            }
        }
        public void Reset()
        {
            if(hostSession.Length>0 || lastPublished.Length>0 || lobbyId!=0)session=Guid.NewGuid().ToString("N");
            HostAvailable=PeerMod=false;hostSession=remoteSession=lastPublished="";desiredKind=requestedCast=seenCast=0;requestedOwner=-1;lobbyId=peerId=0;
        }
    }
}

