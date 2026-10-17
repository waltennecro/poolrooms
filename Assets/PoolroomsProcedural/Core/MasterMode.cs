using System;
using System.Globalization;

namespace Poolrooms.Core
{
    [Serializable]
    public sealed class MasterOptions
    {
        public bool flight=true,noclip=true;
        public float speed=30,boost=3;
        public MasterViewOptions view=new MasterViewOptions();
        public void Sanitize()
        {
            speed=Finite(speed)?Math.Max(5,Math.Min(250,speed)):30;
            boost=Finite(boost)?Math.Max(1,Math.Min(8,boost)):3;
            if(noclip)flight=true;
            if(view==null)view=new MasterViewOptions();view.Sanitize();
        }
        private static bool Finite(float value){return !float.IsNaN(value)&&!float.IsInfinity(value);}
    }
    public enum MasterCommandAction { None, Changed, Unlock, Lock, Menu }
    public readonly struct MasterCommandResult
    {
        public readonly bool IsCommand,Success;
        public readonly MasterCommandAction Action;
        public readonly string Message;
        public MasterCommandResult(bool command,bool success,MasterCommandAction action,string message)
        {IsCommand=command;Success=success;Action=action;Message=message;}
    }
    // Local game commands only. No shell, network or persisted administrator entitlement.
    public sealed class MasterSession
    {
        public bool Unlocked { get; private set; }
        public MasterOptions Options { get; private set; }=new MasterOptions();
        public bool Flying { get { return Unlocked&&Options.flight; } }
        public bool Noclip { get { return Flying&&Options.noclip; } }
        public void Lock(){Unlocked=false;}
        public void SetFlight(bool value){if(!Unlocked)return;Options.flight=value;if(!value)Options.noclip=false;}
        public void SetNoclip(bool value){if(!Unlocked)return;Options.noclip=value;if(value)Options.flight=true;}
        public void ResetOptions(){if(Unlocked)Options=new MasterOptions();}
        public MasterCommandResult Submit(string input)
        {
            if(input==null)input="";input=input.Trim();
            if(input.Length>256)return Result(false,"El mensaje debe tener como máximo 256 caracteres.");
            if(!input.StartsWith("/",StringComparison.Ordinal))return new MasterCommandResult(false,true,MasterCommandAction.None,input);
            string[] words=input.Split(new[]{' ','\t'},StringSplitOptions.RemoveEmptyEntries);
            string name=words[0].ToLowerInvariant();
            if(name=="/help"&&words.Length==1)return Result(true,"/unlock mastermode · /lock mastermode · /master · /fly · /noclip · /speed 30 · /boost 3");
            if(name=="/unlock"&&words.Length==2&&words[1].Equals("mastermode",StringComparison.OrdinalIgnoreCase))
            {
                if(Unlocked)return new MasterCommandResult(true,true,MasterCommandAction.Menu,"El modo administrador ya está activo.");
                Unlocked=true;Options.Sanitize();return new MasterCommandResult(true,true,MasterCommandAction.Unlock,"Modo administrador activado para esta sesión.");
            }
            if(name=="/lock"&&words.Length==2&&words[1].Equals("mastermode",StringComparison.OrdinalIgnoreCase))
            {
                if(!Unlocked)return Result(false,"El modo administrador ya está desactivado.");
                Lock();return new MasterCommandResult(true,true,MasterCommandAction.Lock,"Modo administrador desactivado.");
            }
            if(name!="/master"&&name!="/fly"&&name!="/noclip"&&name!="/speed"&&name!="/boost")return Result(false,"Comando desconocido. Usa /help.");
            if(!Unlocked)return Result(false,"Primero usa /unlock mastermode.");
            if(name=="/master"&&words.Length==1)return new MasterCommandResult(true,true,MasterCommandAction.Menu,"Modo administrador");
            if((name=="/fly"||name=="/noclip")&&words.Length==1)
            {
                if(name=="/fly")SetFlight(!Flying);else SetNoclip(!Noclip);
                return new MasterCommandResult(true,true,MasterCommandAction.Changed,name=="/fly"?(Flying?"Vuelo activado.":"Vuelo desactivado en tu posición."):(Noclip?"Atravesar paredes activado.":"Colisiones activadas en tu posición."));
            }
            if((name=="/speed"||name=="/boost")&&words.Length==2)
            {
                float value;float min=name=="/speed"?5:1,max=name=="/speed"?250:8;
                if(!float.TryParse(words[1].Replace(',','.'),NumberStyles.Float,CultureInfo.InvariantCulture,out value)||float.IsNaN(value)||float.IsInfinity(value)||value<min||value>max)
                    return Result(false,"Usa un valor entre "+min+" y "+max+".");
                if(name=="/speed")Options.speed=value;else Options.boost=value;
                return new MasterCommandResult(true,true,MasterCommandAction.Changed,name=="/speed"?"Velocidad: "+value.ToString("0.#",CultureInfo.InvariantCulture)+" m/s.":"Impulso Shift: ×"+value.ToString("0.#",CultureInfo.InvariantCulture)+".");
            }
            return Result(false,"Formato inválido. Usa /help.");
        }
        private static MasterCommandResult Result(bool success,string message){return new MasterCommandResult(true,success,MasterCommandAction.None,message);}
        public V3 FlightVelocity(V3 right,V3 forward,float sideways,float ahead,float vertical,bool boost)
        {
            if(!Flying)return new V3();Options.Sanitize();
            V3 direction=right*Axis(sideways)+forward*Axis(ahead)+V3.Up*Axis(vertical);
            if(direction.Length>1)direction=direction.Unit;
            return direction*(Options.speed*(boost?Options.boost:1));
        }
        private static float Axis(float value){return float.IsNaN(value)||float.IsInfinity(value)?0:Math.Max(-1,Math.Min(1,value));}
    }
}
