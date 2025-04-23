from flask import Flask, request, jsonify
import music21  # make sure music21 is installed
import os

os.environ["FLASK_ENV"] = "production"
app = Flask(__name__)

@app.route('/analyze', methods=['POST'])
def analyze():
    data = request.get_json()
    if data is not None and 'notes' in data:
        notes = data['notes']
    else:
        return jsonify({"error": "No data provided"}), 400
    
    try:
        chordName = music21.chord.Chord(notes).commonName
        rootNote = music21.chord.Chord(notes).root().name
        print(f"Received notes: {notes}, Chord name: {chordName}")  # Log request details
        return jsonify({"result": chordName, "rootNote": str(rootNote)})
    except Exception as e:
        return jsonify({"error": str(e)}), 400

if __name__ == '__main__':
    app.run(host="127.0.0.1", port=5000)