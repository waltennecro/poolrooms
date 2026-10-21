"""Extract eight distinct, attributed usable Bistro objects, with two LODs each.

Original binary stays in a reproducible external cache. Only converted runtime
resources, hashes and provenance are shipped under Assets; no whole scene dump.
"""
from pathlib import Path
import argparse,hashlib,json
import numpy as np
from PIL import Image
import import_bistro_sources as bistro
from import_city_sources import convert
ROOT=Path(__file__).resolve().parents[1]
DEST=ROOT/'Assets/PoolroomsProcedural/Resources/JourneySources'
MODELS=[('CoatRackA','paris_coat_rack_01a1',1.7),('CoatRackB','paris_coat_rack_01b2',.42),
        ('Basket','paris_wickerbasket_01a1',.36),('PlateA','paris_plate_01a46',.25),
        ('PlateB','paris_plate_01b46',.25),('NapkinPile','Napkin_Pile',.25),
        ('Radiator','paris_radiator_4',.85),('PlantPot','Paris_Plant_Pot_01',.8)]
def main():
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--cache',type=Path,required=True);args=parser.parse_args()
    (DEST/'Licenses').mkdir(parents=True,exist_ok=True)
    lock=DEST/'sources.lock.json';bistro.locked=json.loads(lock.read_text()) if lock.exists() else {}
    doc=json.loads(bistro.acquire(bistro.BASE+'bistro.gltf',args.cache));binary=bistro.acquire(bistro.BASE+'bistro.bin',args.cache)
    assert hashlib.sha256(binary).hexdigest()=='5fb11a1d61aeb2537197aa97833ff950638c9d42451ee4fafbc9550f03918faa'
    reports=[]
    for short,node,length in MODELS:
        glb,specular,details=bistro.extract(doc,binary,node,args.cache,max_texture_size=4096)
        report=convert('J_'+short,glb,'JourneySources',DEST,False,'CC-BY-4.0',False,[(1,48,24),(2,24,12)])
        for m,im in zip(report['materials'],specular):
            if im is None:continue
            resource='JourneySources/Textures/'+m['name']+'_specular';im.save(ROOT/'Assets/PoolroomsProcedural/Resources'/(resource+'.png'))
            m.update(workflow='specular',specular=resource)
            # Avoid a second roughness->gloss byte quantization in the adapter.
            maskPath=ROOT/'Assets/PoolroomsProcedural/Resources'/(m['mask']+'.png')
            mask=np.array(Image.open(maskPath));mask[:,:,3]=np.asarray(im)[:,:,3];Image.fromarray(mask).save(maskPath)
        report.update(details,sourceNode=node,authors=['Amazon.com, Inc. or its affiliates'],modelLicense='CC-BY-4.0',
            sourceUrl=bistro.BASE+'bistro.gltf',upstreamRevision=bistro.REV,sourceSha256=hashlib.sha256(glb).hexdigest(),
            gameplayLongest=length,nativeUnityValidated=False,adaptation='Selected original object; metric rebasing, native PBR maps capped at 4K, preserved specular/gloss workflow, two LODs. No scene dump.')
        (DEST/('J_'+short+'.json')).write_text(json.dumps(report,indent=2)+'\n');reports.append(report)
        print(short,report['triangles'],report['minimum'],report['maximum'],flush=True)
    (DEST/'catalog.json').write_text(json.dumps({'distinctSourceModels':len(reports),'lodsCountAsModels':False,'models':reports,'nativeUnityValidated':False},indent=2)+'\n')
    for name in ['Bistro_CC_BY_4.txt','upstream_MIT.txt']:(DEST/'Licenses'/name).write_bytes((ROOT/'Sources/Bistro'/name).read_bytes())
    (DEST/'Licenses/NOTICE.txt').write_text('Eight selected objects from Amazon Lumberyard Bistro, Amazon.com, Inc. or its affiliates, CC BY 4.0.\nSource: https://developer.nvidia.com/orca/amazon-lumberyard-bistro\nPinned derivative: '+bistro.BASE+'bistro.gltf\nChanges: selected object extraction/rebasing, PBR exports up to native 4K, OBJ and two LOD derivatives. No original lights, animations or full-scene binary bundled. Source URLs and hashes: sources.lock.json.\n')
    lock.write_text(json.dumps(bistro.locked,indent=2)+'\n')
    hashes={str(p.relative_to(DEST)):{'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()}
            for p in sorted(DEST.rglob('*')) if p.is_file() and p.name!='files.sha256.json'}
    (DEST/'files.sha256.json').write_text(json.dumps(hashes,indent=2)+'\n')
if __name__=='__main__':main()
