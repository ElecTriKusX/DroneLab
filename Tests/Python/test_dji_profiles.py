import importlib.util
import json
from pathlib import Path
import struct
import unittest

ROOT=Path(__file__).resolve().parents[2]
spec=importlib.util.spec_from_file_location('build_dji_profiles',ROOT/'Tools/build_dji_profiles.py')
dji=importlib.util.module_from_spec(spec)
spec.loader.exec_module(dji)

class DjiProfileTests(unittest.TestCase):
    def test_hierarchy_paths_follow_siblings_not_duplicate_names(self):
        j={'nodes':[{'name':'root','children':[1,2]},{'name':'same','mesh':0},{'name':'same','mesh':0,'translation':[2,0,0]}], 'scenes':[{'nodes':[0]}], 'meshes':[{'primitives':[{'attributes':{'POSITION':0}}]}], 'accessors':[{'min':[-1,-1,-1],'max':[1,1,1]}]}
        bounds,world,paths=dji.geometry(j)
        self.assertEqual(paths[1],'@/0/0')
        self.assertEqual(paths[2],'@/0/1')
        self.assertEqual(bounds[0],[[-1,-1,-1],[3,1,1]])
        self.assertEqual(dji.transform(world[2],[0,0,0]),[2,0,0])

    def test_avata_derivative_preserves_original_mesh_and_adds_four_independent_props(self):
        j={'nodes':[{'name':'original'}],'scenes':[{'nodes':[0]}],'buffers':[{'byteLength':4}], 'bufferViews':[], 'accessors':[], 'materials':[], 'meshes':[]}
        result,data,nodes=dji.avata_propellers(j,b'1234')
        self.assertEqual(j['nodes'],[{'name':'original'}])
        self.assertEqual(result['nodes'][0],j['nodes'][0])
        self.assertEqual(len(nodes),4)
        self.assertEqual(len(set(result['nodes'][i]['name'] for i in nodes)),4)
        magic,version,length=struct.unpack_from('<III',data)
        self.assertEqual((magic,version,length),(0x46546c67,2,len(data)))
        bounds,world,paths=dji.geometry(result)
        self.assertEqual(paths[nodes[0]],'@/1')
        for node in nodes:
            lo,hi=bounds[node]
            self.assertLessEqual(max(b-a for a,b in zip(lo,hi)),.0762)
        position=result['accessors'][0];normal=result['accessors'][1]
        self.assertEqual(position['count'],normal['count'])
        size=struct.unpack_from('<I',data,12)[0];binary=data[20+size+8:]
        self.assertEqual(binary[:4],b'1234')
        for node in nodes:self.assertNotIn('children',result['nodes'][node])

    def test_published_data_and_m600_equivalent_are_distinguished(self):
        specs=dji.SPECS
        self.assertEqual(specs['dji_m600_ul_ver']['cells'],6)
        self.assertEqual(specs['dji_m600_ul_ver']['capacity'],6*4.5)
        self.assertEqual(len(specs['dji_m600_ul_ver']['parts']),6)
        for model in specs:
            profile=json.loads((ROOT/'Assets/DronePhysics/Resources/DronePhysics'/(model+'_approx.json')).read_text(encoding='utf-8'))
            self.assertNotIn('environment',profile)
            self.assertTrue(any(p['sourceType']=='Estimated' for p in profile['parameterProvenance']))
            self.assertTrue(any(p['sourceType']=='Manufacturer' for p in profile['parameterProvenance']))

if __name__=='__main__':unittest.main()
