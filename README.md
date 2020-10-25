### Backend/fancy proxy and frontend for a Garage48 hackathon from Dreamteam

Model server, that does actual predictions, can be found here: https://github.com/random-alex/hackathon2020

Main idea for the solution - capture video with a MediaRecorder on frontend - either from webcam, or by uploading a file, 
and send chunks using websockets to the backend. On a backend, chunks are buffered, and once there are enough, 
are converted to a larger mp4 file (~10 seconds in length). Those mp4 files are then continously sent to model server. 
Response from a the server are sent back as soon as they are received and are shown under received video (simulating, for example, a video call)

Backend is done with ASP.NET Core 3.1 and raw WebSockets.
Frontent is done with Vue 2
