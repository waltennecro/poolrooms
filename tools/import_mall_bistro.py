"""Six additional attributed Bistro retail/cafe components, native maps up to 4K."""
from pathlib import Path
import argparse,hashlib,json
import import_bistro_sources as bistro
from import_city_sources import convert
ROOT=Path(__file__).resolve().parents[1];SOURCE=ROOT/'Sources/MallBistro';DEST=ROOT/'Assets/PoolroomsProcedural/Resources/MallSources'
MODELS=[('IndoorChair','Paris_Bistro_Interior_Chair_01'),('IndoorTable','Paris_Bistro_Interior_Table_Large_01'),
 ('CashRegister','paris_cashregister_01a1'),('Stool','paris_barstool_01a1'),('Napkins','NapkinHolder_01'),('MenuSign','MenuSign_Small_01')]
def main():
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--cache',type=Path,required=True);args=parser.parse_args()
    SOURCE.mkdir(exist_ok=True);lockfile=SOURCE/'sources.lock.json';bistro.locked=json.loads(lockfile.read_text()) if lockfile.exists() else {}
    original=bistro.acquire(bistro.BASE+'bistro.gltf',args.cache);binary=bistro.acquire(bistro.BASE+'bistro.bin',args.cache);doc=json.loads(original)
    assert hashlib.sha256(binary).hexdigest()=='5fb11a1d61aeb2537197aa97833ff950638c9d42451ee4fafbc9550f03918faa'
    catalog=json.loads((DEST/'catalog.json').read_text());reports=[m for m in catalog['models'] if m['id'] not in {'M_'+name for name,_ in MODELS}]
    for short,node in MODELS:
        glb,specular,details=bistro.extract(doc,binary,node,args.cache,max_texture_size=4096);(SOURCE/(short+'_adapted.glb')).write_bytes(glb)
        report=convert('M_'+short,glb,'MallSources',DEST,False,'CC-BY-4.0',False,[(1,48,24),(2,24,12)])
        for material,image in zip(report['materials'],specular):
            if image is None:continue
            resource='MallSources/Textures/'+material['name']+'_specular';image.save(ROOT/'Assets/PoolroomsProcedural/Resources'/(resource+'.png'));material.update(workflow='specular',specular=resource)
        report.update(details,authors=['Amazon.com, Inc. or its affiliates'],owners=['Amazon.com, Inc. or its affiliates'],sourceName=short,modelLicense='CC-BY-4.0',sourceSha256=hashlib.sha256(glb).hexdigest(),sourceUrl=bistro.BASE+'bistro.gltf',upstreamRevision=bistro.REV,nativeUnityValidated=False,adaptation='Six selected original Bistro components; metric rebasing, native OBJ/two LODs, original source texture resolution up to 4096, authored specular/gloss workflow. Not six complete scene downloads.')
        (DEST/('M_'+short+'.json')).write_text(json.dumps(report,indent=2)+'\n');reports.append(report)
        print(short,report['triangles'],report['minimum'],report['maximum'],flush=True)
    (DEST/'catalog.json').write_text(json.dumps({'distinctSourceModels':len(reports),'lodsCountAsModels':False,'models':reports,'nativeUnityValidated':False},indent=2)+'\n')
    for name in ['Bistro_CC_BY_4.txt','upstream_MIT.txt','NOTICE.txt']:(DEST/'Licenses'/('Bistro_'+name)).write_bytes((ROOT/'Sources/Bistro'/name).read_bytes())
    (DEST/'Licenses/NOTICE.txt').write_text((DEST/'Licenses/NOTICE.txt').read_text()+'\nAdditional Bistro components: interior chair, interior table, cash register, stool, napkin holder, menu board. Amazon.com, Inc. or its affiliates, CC BY 4.0. Changes: selected/rebased OBJ/PBR exports and two LODs each. See Bistro source licence and pinned catalog.\n')
    # Keep selected original map bytes for reproducible adaptation. Shared repeated
    # source maps are copied once; the original complete 95 MB scene stays cached.
    for key in list(bistro.locked):
        path=args.cache/key
        if path.suffix.lower()!='.dds':continue
        out=SOURCE/'OriginalMaps'/key.split('/'+bistro.REV+'/',1)[1];out.parent.mkdir(parents=True,exist_ok=True);out.write_bytes(path.read_bytes())
    lockfile.write_text(json.dumps(bistro.locked,indent=2)+'\n')
    manifest={p.relative_to(DEST).as_posix():{'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in sorted(DEST.rglob('*')) if p.is_file() and p.name!='files.sha256.json'}
    (DEST/'files.sha256.json').write_text(json.dumps(manifest,indent=2)+'\n')
if __name__=='__main__':main()
