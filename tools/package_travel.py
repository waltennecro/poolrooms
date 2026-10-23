"""Package a cumulative travel update over Cinco horizontes or newer Assets."""
import argparse,hashlib,json,subprocess,zipfile
from pathlib import Path
BASE='b6e631b39f01915fcd367cf3abd4106e27c0707a'
ROOT=Path(__file__).resolve().parents[1]
def main():
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('--output',required=True,type=Path);a=p.parse_args();out=a.output.resolve()
 if out.exists() or out.is_relative_to(ROOT/'Assets'):raise SystemExit('Choose a new output outside Assets')
 def git(*args):return subprocess.check_output(['git',*args],cwd=ROOT,text=True).strip()
 if git('status','--porcelain'):raise SystemExit('Commit reviewed source before packaging')
 revision=git('rev-parse','HEAD');files={}
 for entry in git('diff','--name-status',BASE,revision,'--','Assets/PoolroomsProcedural').splitlines():
  kind,name=entry.split('\t',1)
  if kind not in ('A','M'):raise SystemExit('Explicit removal/rename instructions required: '+entry)
  files[Path(name).relative_to('Assets').as_posix()]=(ROOT/name).read_bytes()
 assert 'PoolroomsProcedural/Runtime/PoolroomsTravel.cs' in files
 manifest={'sourceRevision':revision,'baseRevision':BASE,'workingTreeChanged':False,'fullAssetsRequired':True,'nativeUnityExecuted':False,
  'compatibleReleases':['cinco-horizontes','correccion-compilacion-runtime','rutas-flexibles','hotel-ventanas-skins','cygnus-reconstruido','invernadero-cristal','nivel63-tranquilidad','superficies-estables','nivel11-dreamcore-provisiones','nivel11-arquitectura-256','nivel39-bosque-encantado'],
  'files':{n:{'bytes':len(d),'sha256':hashlib.sha256(d).hexdigest()} for n,d in sorted(files.items())}}
 files['PARCHE_MANIFIESTO.json']=(json.dumps(manifest,indent=2)+'\n').encode()
 files['LEEME.txt']=('Travesia libre: C ritmo Normal/Agil/Expres, J avance automatico, K o boton central mantenido para gancho, tecla de correr para nado rapido. No requiere maestro. Controles en ajustes y menu principal.\n'
  'Actualizacion acumulativa desde Cinco horizontes. Incluye todo lo posterior: hotel, Cygnus, invernadero, Nivel63, superficies estables, comida/Nivel11/256 edificios, Bosque encantado y movilidad.\n'
  'Con Unity detenido, copia PoolroomsProcedural sobre tu unica carpeta existente en Assets. Agrega/sobrescribe y conserva los demas recursos y .meta. No instales otra copia paralela. Si no tienes una base compatible, usa los Assets completos.\n'
  'No hace falta regenerar salas por la movilidad. Se preserva el contenido, la linterna y distancia de renderizado. Unity/PlayMode/HDRP/PhysX, imagen y FPS nativos pendientes.\n').encode()
 out.parent.mkdir(parents=True,exist_ok=True)
 with zipfile.ZipFile(out,'w',compression=zipfile.ZIP_DEFLATED) as z:
  for name,data in sorted(files.items()):
   info=zipfile.ZipInfo(name,(2026,1,1,0,0,0));info.compress_type=zipfile.ZIP_DEFLATED;z.writestr(info,data)
 with zipfile.ZipFile(out) as z:assert z.testzip() is None and all(z.read(n)==d for n,d in files.items())
 print(json.dumps({'zip':str(out),'files':len(manifest['files']),'bytes':out.stat().st_size,'sha256':hashlib.sha256(out.read_bytes()).hexdigest(),'sourceRevision':revision,'workingTreeChanged':False}))
if __name__=='__main__':main()
