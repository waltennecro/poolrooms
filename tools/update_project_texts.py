"""Synchronise the user's three project TXT documents with the gameplay journal.

The original assessment PDF remains a dated 50-page snapshot. Its TXT carries
an explicitly later journal and keeps the original 49 form-feed boundaries.
"""
import argparse,json,re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
BEGIN='[INICIO DEL REGISTRO VIVO DE CAMBIOS]'
END='[FIN DEL REGISTRO VIVO DE CAMBIOS]'
FILES=['GUIA_MAESTRA_PROYECTO_POOLROOMS.txt','LO_QUE_LE_FALTA_AL_JUEGO_50_PAGINAS.txt','LEER_PRIMERO_PARA_LA_SIGUIENTE_IA.txt']
def strip_block(text):
 pattern=re.escape(BEGIN)+r'.*?'+re.escape(END)+r'\n*'
 return re.sub(pattern,'',text,flags=re.S)
def render(data):
 lines=['REGISTRO VIVO DE CAMBIOS DEL JUEGO','Fuente: CAMBIOS_JUEGO_DESDE_RELEVO.json. Zona horaria: '+data['timezone'],data['requirement'],
  'Los capítulos/manuales anteriores son instantáneas históricas. Este registro prevalece para conocer cambios posteriores. El PDF original conserva sus 50 páginas; el TXT del plan añade esta bitácora.','']
 for entry in data['entries']:
  lines.extend([entry['date']+' · '+entry['id'], 'Estado: '+entry['status']])
  if entry.get('sourceRevision'):lines.append('Fuente exacta: '+entry['sourceRevision'])
  for label,key in [('Cambios','changes'),('Comprobaciones reales','verification'),('Pendiente','pending')]:
   lines.append(label+':');lines.extend('  - '+value for value in entry[key])
  lines.append('Guía: '+entry['documentation']);lines.append('')
 return '\n'.join(lines)+'\n'
def outputs():
 data=json.loads((ROOT/'CAMBIOS_JUEGO_DESDE_RELEVO.json').read_text())
 assert data['entries'] and len({x['id'] for x in data['entries']})==len(data['entries'])
 block=BEGIN+'\n'+render(data)+END+'\n\n'
 contents={name:strip_block((ROOT/name).read_text(encoding='utf-8-sig')) for name in FILES}
 # Keep the embedded handoff and the separately downloadable first-read note aligned.
 marker='='*79+'\nANEXO K. RELEVO PARA LA SIGUIENTE IA\n'+'='*79+'\n\n'
 finish='='*79+'\nFIN DEL DOCUMENTO\n'+'='*79
 handoff_title,handoff_body=contents[FILES[2]].split('\n',1)
 contents[FILES[2]]=handoff_title+'\n\n'+handoff_body.lstrip('\n')
 guide=contents[FILES[0]];assert guide.count(marker)==1 and guide.count(finish)==1
 handoff_end='[FIN DEL ANEXO K]'
 end=guide.index(handoff_end) if handoff_end in guide else guide.index(finish)
 before=guide[:guide.index(marker)+len(marker)];after=guide[end:] if handoff_end in guide else handoff_end+'\n\n'+guide[end:]
 contents[FILES[0]]=before+contents[FILES[2]].rstrip()+'\n\n'+after
 result={}
 for name,text in contents.items():
  first,body=text.split('\n',1);value=first+'\n\n'+block+body.lstrip('\n')
  assert value.count(BEGIN)==value.count(END)==1
  if name==FILES[1]:assert value.count('\f')==49
  if name==FILES[0]:assert len(re.findall(r'^FICHA \d{2}/\d{2} — ',value,re.M))==data.get('documentedLevelCards',66)
  result[name]=b'\xef\xbb\xbf'+value.encode('utf-8')
 result['CAMBIOS_JUEGO_DESDE_RELEVO.txt']=b'\xef\xbb\xbf'+render(data).encode('utf-8')
 return result

def main():
 parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--check',action='store_true');a=parser.parse_args()
 result=outputs()
 if a.check:
  wrong=[name for name,content in result.items() if not (ROOT/name).is_file() or (ROOT/name).read_bytes()!=content]
  if wrong:raise SystemExit('TXT out of sync: '+', '.join(wrong))
  print('PASS: all three TXT documents and the public journal match the gameplay inventory; documented level cards and 50 assessment sections preserved.')
 else:
  for name,content in result.items():(ROOT/name).write_bytes(content)
  print('Updated '+', '.join(result))
if __name__=='__main__':main()
