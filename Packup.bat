cd HomeworkViewer
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
xcopy C:\Users\MaxSui\Documents\HomeworkViewer7\bin\Debug\net6.0-windows\Resources C:\Users\MaxSui\Documents\HomeworkViewer7\bin\Release\net6.0-windows\win-x64\Resources /s /e /y
cd C:\Users\MaxSui\Documents\HomeworkViewer7\bin\Release\net6.0-windows\
ren C:\Users\MaxSui\Documents\HomeworkViewer7\bin\Release\net6.0-windows\win-x64 HomeworkViewer