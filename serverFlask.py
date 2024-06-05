from flask import Flask, request, jsonify
import music21  # make sure music21 is installed

app = Flask(__name__)

@app.route('/analyze', methods=['POST'])
def analyze():
    data = request.get_json()
    if data is not None and 'notes' in data:
        notes = data['notes']
    else:
        return jsonify({"error": "No data provided"}), 400
        
    chordName = music21.chord.Chord(notes).commonName
    print(f"Received notes: {notes}, Chord name: {chordName}")  # Log request details
    return jsonify({"result": chordName})

if __name__ == '__main__':
    app.run(debug=True, port=5000)
