import { ChangeEvent, useState } from "react";
import { AiOutlinePlusCircle } from 'react-icons/ai';

interface CreatedTodo {
    handleAdd: (description: string) => void;
}

export const CreatedTodo: React.FC<CreatedTodo> = ({ handleAdd }) => {
    const [value, setValue] = useState<string>('');

    const onChange = (event: ChangeEvent<HTMLInputElement>) => {
        setValue(event.target.value);
    }

    const onClick = async () => {
        if (value == '') {
            alert('Digite a descrição da atividade.');
            return;
        }

        await handleAdd(value);
        setValue('');
    }

    return (
        <div className=' flex w-64 justify-between items-center mt-4'>
            <div className='bg-slate-900 border-solid border-2 border-slate-700 h-auto max-w-lg w-10/12 rounded-sm'>
                <input maxLength={64} value={value} onChange={event => onChange(event)} placeholder='Adicione uma atividade' className='p-2 w-full break-words bg-transparent focus:outline-none' />
            </div>
            <button onClick={onClick} className='w-6 h-6 ml-2'>
                <AiOutlinePlusCircle style={{ color: 'green', fontSize: 24 }} />
            </button>
        </div>
    )
}