"""Generate stable Core food dimensions directly from licensed imported bounds."""
from pathlib import Path
import json
ROOT=Path(__file__).resolve().parents[1];base=ROOT/'Assets/PoolroomsProcedural'
rows=json.loads((ROOT/'Sources/Food/selection.json').read_text())
lines=['// Generated from pinned FoodSources bounds; regenerate with tools/generate_food_catalog.py.','namespace Poolrooms.Core','{','    public static partial class ProvisionCatalog','    {','        private static readonly ProvisionDefinition[] entries={']
for row in rows:
    d=json.loads((base/'Resources/FoodSources'/(row['resourceId']+'.json')).read_text());size=[b-a for a,b in zip(d['minimum'],d['maximum'])];scale=min(.38/max(size),1);size=[v*scale for v in size]
    lines.append('            new ProvisionDefinition('+str(row['id'])+','+json.dumps(row['name'],ensure_ascii=False)+','+json.dumps(row['resourceId'])+','+json.dumps(row['family'])+','+json.dumps(row['foodType'])+','+str(row['drink']).lower()+',new V3('+','.join(format(v,'.9g')+'f' for v in size)+')),')
lines+=['        };','    }','}'];(base/'Core/ProvisionCatalog.Generated.cs').write_text('\n'.join(lines)+'\n')
