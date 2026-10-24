"""Package the complete country update, cumulatively or over Travesía libre."""
import argparse,hashlib,json,subprocess,zipfile
from pathlib import Path
BASE='b6e631b39f01915fcd367cf3abd4106e27c0707a'
ROOT=Path(__file__).resolve().parents[1]
def main():
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('--output',required=True,type=Path);p.add_argument('--from-travel',action='store_true');a=p.parse_args();base='4e2100a7d9cc62553472d4f4fa751930870b7c4f' if a.from_travel else BASE;out=a.output.resolve()
 if out.exists() or out.is_relative_to(ROOT/'Assets'):raise SystemExit('Choose a new output outside Assets')
 def git(*args):return subprocess.check_output(['git',*args],cwd=ROOT,text=True).strip()
 if git('status','--porcelain'):raise SystemExit('Commit reviewed source before packaging')
 revision=git('rev-parse','HEAD');files={}
 for entry in git('diff','--name-status',base,revision,'--','Assets/PoolroomsProcedural').splitlines():
  kind,name=entry.split('\t',1)
  if kind not in ('A','M'):raise SystemExit('Explicit removal/rename instructions required: '+entry)
  files[Path(name).relative_to('Assets').as_posix()]=(ROOT/name).read_bytes()
 assert 'PoolroomsProcedural/Core/Level10Plan.cs' in files and 'PoolroomsProcedural/Resources/Level10/Textures/Siding_Height.png' in files
 manifest={'sourceRevision':revision,'baseRevision':base,'workingTreeChanged':False,'fullAssetsRequired':True,'nativeUnityExecuted':False,
  'compatibleReleases':['travesia-libre'] if a.from_travel else ['travesia-libre','cinco-horizontes','correccion-compilacion-runtime','rutas-flexibles','hotel-ventanas-skins','cygnus-reconstruido','invernadero-cristal','nivel63-tranquilidad','superficies-estables','nivel11-dreamcore-provisiones','nivel11-arquitectura-256','nivel39-bosque-encantado'],
  'files':{n:{'bytes':len(d),'sha256':hashlib.sha256(d).hexdigest()} for n,d in sorted(files.items())}}
 files['PARCHE_MANIFIESTO.json']=(json.dumps(manifest,indent=2)+'\n').encode()
 files['LEEME.txt']=(
  'Nivel 10 - Campos de abundancia. Nuevo campo procedural en las afueras del Nivel11, doce programas, tres cubiertas, altillos, lagunas y actividades de granja. Tres conjuntos CC0 nuevos (239.739.811 bytes PBR) y cuatro clips originales. No hay modelos externos nuevos ni relleno.\n'
  +('Actualiza los Assets completos de Travesia libre. No es para una base anterior.\n' if a.from_travel else 'Parche acumulativo desde Cinco horizontes o posteriores, incluida Travesia libre. Conserva bosque39 y todas las mejoras intermedias.\n')
  +'Con Unity detenido, copia PoolroomsProcedural sobre tu UNICA carpeta existente dentro de Assets. Sobrescribe y agrega archivos, conserva los demas recursos y .meta. No instales dos copias paralelas. Si no tienes una base compatible, usa los Assets completos de esta entrega.\n'
  +'Reinicia Play para generar el mundo actualizado. Las rutas de las afueras del Nivel11 llevan al nuevo campo. La exploracion unificada lo activa; perfiles personalizados: enableWheatFields10. E usa mecanismos y recoge recuerdos; Tab inventario; C ritmos de travesia; J avance automatico; K/boton central gancho.\n'
  +'Unity/PlayMode/HDRP, shaders, PhysX, aspecto, escucha, tacto y FPS nativos pendientes. Las comprobaciones de nucleo/codigo/recursos no equivalen a ejecutarlo en Unity.\n').encode()
 out.parent.mkdir(parents=True,exist_ok=True)
 with zipfile.ZipFile(out,'w',compression=zipfile.ZIP_DEFLATED) as z:
  for name,data in sorted(files.items()):
   info=zipfile.ZipInfo(name,(2026,1,1,0,0,0));info.compress_type=zipfile.ZIP_DEFLATED;z.writestr(info,data)
 with zipfile.ZipFile(out) as z:assert z.testzip() is None and all(z.read(n)==d for n,d in files.items())
 print(json.dumps({'zip':str(out),'files':len(manifest['files']),'bytes':out.stat().st_size,'sha256':hashlib.sha256(out.read_bytes()).hexdigest(),'sourceRevision':revision,'workingTreeChanged':False}))
if __name__=='__main__':main()
