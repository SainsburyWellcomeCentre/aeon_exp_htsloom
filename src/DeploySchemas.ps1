
uv run python -m swc.aeon_exp.htsloom.experiment   
uv run python -m swc.aeon_exp.htsloom.feeder

dotnet bonsai.sgen .\HtsLoom.json -o Extensions --serializer yaml --serializer json
dotnet bonsai.sgen .\Feeder.json -o Extensions --serializer yaml --serializer json
