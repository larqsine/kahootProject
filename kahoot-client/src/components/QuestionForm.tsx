import React, { useState } from 'react';

interface Option {
    text: string;
    isCorrect: boolean;
}

interface QuestionFormProps {
    onSubmit: (questionText: string, options: Option[]) => void;
}

const QuestionForm: React.FC<QuestionFormProps> = ({ onSubmit }) => {
    const [questionText, setQuestionText] = useState('');
    const [options, setOptions] = useState<Option[]>([
        { text: '', isCorrect: false },
        { text: '', isCorrect: false },
        { text: '', isCorrect: false },
        { text: '', isCorrect: false }
    ]);

    const handleOptionChange = (index: number, text: string) => {
        const newOptions = [...options];
        newOptions[index].text = text;
        setOptions(newOptions);
    };

    const handleCorrectChange = (index: number) => {
        const newOptions = options.map((option, i) => ({
            ...option,
            isCorrect: i === index
        }));
        setOptions(newOptions);
    };

    const handleSubmit = (e: React.FormEvent) => {
        e.preventDefault();
        const validOptions = options.filter(option => option.text.trim() !== '');
        if (questionText && validOptions.length >= 2) {
            onSubmit(questionText, validOptions);
            setQuestionText('');
            setOptions([
                { text: '', isCorrect: false },
                { text: '', isCorrect: false },
                { text: '', isCorrect: false },
                { text: '', isCorrect: false }
            ]);
        }
    };

    return (
        <div className="card mb-4 p-3">
            <h3>Add Question</h3>
            <form onSubmit={handleSubmit}>
                <div className="mb-3">
                    <label className="form-label">Question Text</label>
                    <input
                        type="text"
                        className="form-control"
                        value={questionText}
                        onChange={(e) => setQuestionText(e.target.value)}
                        required
                    />
                </div>

                <div className="mb-3">
                    <label className="form-label">Options</label>
                    {options.map((option, index) => (
                        <div key={index} className="input-group mb-2">
                            <input
                                type="text"
                                className="form-control"
                                placeholder={`Option ${index + 1}`}
                                value={option.text}
                                onChange={(e) => handleOptionChange(index, e.target.value)}
                            />
                            <div className="input-group-text">
                                <input
                                    type="radio"
                                    name="correctOption"
                                    checked={option.isCorrect}
                                    onChange={() => handleCorrectChange(index)}
                                    className="form-check-input mt-0"
                                />
                                <span className="ms-2">Correct</span>
                            </div>
                        </div>
                    ))}
                </div>

                <button type="submit" className="btn btn-primary">
                    Add Question
                </button>
            </form>
        </div>
    );
};

export default QuestionForm;